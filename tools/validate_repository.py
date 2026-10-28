#!/usr/bin/env python3
"""Dependency-free repository checks. This does not compile or execute Unity."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
ERRORS: list[str] = []
WARNINGS: list[str] = []


def error(message: str) -> None:
    ERRORS.append(message)


def load_json(relative: str):
    path = ROOT / relative
    try:
        return json.loads(path.read_text(encoding='utf-8-sig'))
    except (OSError, ValueError) as exc:
        error(f'{relative}: {exc}')
        return {}


def source_files():
    for path in ROOT.iterdir():
        if path.is_file():
            yield path
    for folder in ('Assets', 'Packages', 'ProjectSettings', 'docs', 'tools', 'tests', '.github', 'Design'):
        base = ROOT / folder
        if base.exists():
            yield from (p for p in base.rglob('*') if p.is_file() and '__pycache__' not in p.parts)
    # Enumerate only portable Unreal inputs; never scan compiler/DDC/installed Epic trees.
    unreal = ROOT / 'Unreal/Pinefall'
    for path in unreal.iterdir() if unreal.exists() else []:
        if path.is_file():
            yield path
    for folder in ('Source', 'Config', 'Scripts', 'SourceArt', 'Content/Pinefall'):
        base = unreal / folder
        if base.exists():
            yield from (p for p in base.rglob('*') if p.is_file() and '__pycache__' not in p.parts)


def validate() -> int:
    required = (
        'README.md', 'AGENTS.md', 'CONTRIBUTING.md', 'LICENSE.md', '.gitignore',
        '.gitattributes', 'Packages/manifest.json', 'Packages/packages-lock.json',
        'ProjectSettings/ProjectVersion.txt', 'ProjectSettings/EditorSettings.asset',
        'Assets/Scenes/Reserve.unity', 'Assets/Resources/DetailedAtlas.png',
        'Assets/Resources/RoadCoverage.png', 'Assets/Generated/ReserveTerrain.asset',
        'docs/SETUP.md', 'docs/ARCHITECTURE.md', 'docs/HANDOFF.md', 'docs/TESTING.md',
        'tools/Build-Game.ps1', 'tools/Test-Game.ps1', 'tools/Measure-Performance.ps1',
        'THIRD-PARTY-NOTICES.md', 'AUDIO-SOURCES.json', 'GAME-GUIDE.md',
        'Design/RULES-CATALOG.md', 'Design/rules.json', 'Design/UNREAL-MIGRATION.md',
        'Design/UNREAL-PRESENTATION.md', 'Design/UNREAL-STATUS.md',
        'Unreal/Pinefall/Pinefall.uproject', 'Unreal/Pinefall/README.md',
        'tools/Setup-Unreal.ps1', 'tools/Build-Unreal.ps1', 'tools/Run-Unreal.ps1',
    )
    for relative in required:
        if not (ROOT / relative).is_file():
            error(f'Missing required source/document: {relative}')

    version_file = ROOT / 'ProjectSettings/ProjectVersion.txt'
    if version_file.exists() and not re.search(r'^m_EditorVersion: 2022\.3\.13f1\s*$', version_file.read_text(), re.M):
        error('Editor version drift: review the pinned 2022.3.13f1 baseline and docs together.')
    settings = ROOT / 'ProjectSettings/EditorSettings.asset'
    if settings.exists() and 'm_SerializationMode: 2' not in settings.read_text():
        error('Use Force Text serialization for reviewable Unity assets.')
    vcs = ROOT / 'ProjectSettings/VersionControlSettings.asset'
    if not vcs.exists() or 'm_Mode: Visible Meta Files' not in vcs.read_text():
        error('Use Visible Meta Files for stable, source-controlled Unity GUIDs.')

    manifest = load_json('Packages/manifest.json').get('dependencies', {})
    lock = load_json('Packages/packages-lock.json').get('dependencies', {})
    for package, expected in {'com.unity.netcode.gameobjects': '1.8.1', 'com.unity.cloud.gltfast': '6.0.1'}.items():
        if manifest.get(package) != expected or lock.get(package, {}).get('version') != expected:
            error(f'{package}: manifest/lock must both pin {expected}; deliberate upgrades need a baseline review.')
    for package, version in manifest.items():
        if package not in lock:
            error(f'Package missing from resolved lock: {package}')
        elif lock[package].get('version') != version:
            error(f'Manifest/lock mismatch: {package}')

    guids: dict[str, Path] = {}
    assets = ROOT / 'Assets'
    if assets.exists():
        for path in assets.rglob('*'):
            if path.name.startswith('.'):
                continue
            relative = path.relative_to(ROOT).as_posix()
            if path.suffix == '.meta':
                asset = Path(str(path)[:-5])
                if not asset.exists():
                    error(f'Orphan .meta: {relative}')
                match = re.search(r'^guid: ([a-f0-9]{32})\s*$', path.read_text(encoding='utf-8-sig'), re.M)
                if not match:
                    error(f'Missing/invalid metadata GUID: {relative}')
                elif match[1] in guids:
                    error(f'Duplicate GUID: {relative} and {guids[match[1]].relative_to(ROOT)}')
                else:
                    guids[match[1]] = path
            elif not Path(str(path) + '.meta').is_file():
                error(f'Missing companion .meta: {relative}')

    files = list(source_files())
    forbidden_names = {'.env', 'credentials.json', 'id_rsa', 'id_ed25519'}
    for path in files:
        relative = path.relative_to(ROOT).as_posix()
        size = path.stat().st_size
        if size >= 100 * 1024 * 1024:
            error(f'File reaches GitHub normal-file limit; agree on LFS/storage first: {relative}')
        elif size > 50 * 1024 * 1024:
            WARNINGS.append(f'Large baseline file (>50 MiB advisory): {relative}')
        if path.name in forbidden_names or path.suffix.lower() in {'.pem', '.pfx', '.key', '.keystore', '.exe', '.zip', '.7z', '.dmp', '.log'}:
            error(f'Private/local/generated file belongs outside source: {relative}')
        if path.suffix in {'.md', '.ps1', '.py', '.yml', '.json', '.cs', '.cpp', '.h', '.ini', '.uproject'} and size < 2 * 1024 * 1024:
            text = path.read_text(encoding='utf-8-sig')
            if re.search(r'(?i)C:[/\\]Users[/\\][^/\\\s]+', text):
                error(f'Personal absolute path in portable source: {relative}')
            # Detect recognizable token formats without printing the token itself.
            if re.search(r'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,})\b', text):
                error(f'Possible credential in {relative}; remove/revoke it before committing.')
        if path.suffix.lower() == '.md':
            text = path.read_text(encoding='utf-8-sig')
            # Validate ordinary Markdown links/images, excluding external URLs and anchors.
            for match in re.finditer(r'!?\[[^\]\n]*\]\(([^\n)]+)\)', text):
                target = match[1].strip()
                if target.startswith('<'):
                    target = target[1:target.find('>')]
                else:
                    target = target.split(' "', 1)[0]
                if not target or target.startswith('#') or re.match(r'^[a-zA-Z][a-zA-Z0-9+.-]*:', target):
                    continue
                local = unquote(target.split('#', 1)[0].split('?', 1)[0])
                if local and not (path.parent / local).exists():
                    error(f'Broken local documentation link: {relative} -> {local}')

    # Historical evidence is immutable data, and must be internally consistent.
    baseline = ROOT / 'tests/baselines/0.7.1'
    for folder, expected_reports, expected_passes in [('gameplay', 10, 146), ('compatibility', 3, 32)]:
        reports = sorted((baseline / folder).glob('*.json'))
        total = 0
        if len(reports) != expected_reports:
            error(f'Historical {folder} baseline expects {expected_reports} reports, found {len(reports)}.')
        for report in reports:
            data = load_json(report.relative_to(ROOT).as_posix())
            total += len(data.get('passed', []))
            if data.get('failed') or data.get('errors'):
                error(f'Historical report records failures/errors: {report.relative_to(ROOT)}')
        if total != expected_passes:
            error(f'Historical {folder} assertion count changed: {total} versus {expected_passes}.')

    for warning in WARNINGS:
        print(f'WARNING: {warning}')
    for message in ERRORS:
        print(f'ERROR: {message}')
    print(f'Checked {len(files)} source/doc/evidence files and {len(guids)} Unity GUIDs. '
          f'{len(ERRORS)} errors, {len(WARNINGS)} warnings. No engine compilation performed.')
    return 1 if ERRORS else 0


if __name__ == '__main__':
    sys.exit(validate())
