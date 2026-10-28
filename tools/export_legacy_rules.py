from pathlib import Path
import json,hashlib,re
root=Path(__file__).resolve().parents[1];out=root/'Design'/'Legacy';out.mkdir(parents=True,exist_ok=True)
def read(n):return (root/n).read_text(encoding='utf-8-sig')
def objects(n,typename):
 s=read(n);items=[]
 for body in re.findall(r'new '+typename+r'\s*\{([^{}]*)\}',s):
  d={}
  for key,val in re.findall(r'(\w+)\s*=\s*("(?:[^"\\]|\\.)*"|[\w.]+|[\d.-]+f?)',body):
   if val.startswith('"'):value=json.loads(val)
   elif val in ['true','false']:value=val=='true'
   else:
    try:value=float(val.rstrip('f')) if '.' in val else int(val.rstrip('f'))
    except ValueError:value=val.split('.')[-1]
   d[key]=value
  items.append(d)
 return items
weapons=[w for w in objects('Assets/Scripts/WeaponData.cs','WeaponData') if 'category' in w]
kits=objects('Assets/Scripts/ExpeditionLayout.cs','StartingKit')
mods=read('Assets/Scripts/WeaponMods.cs');names=json.loads('['+re.search(r'Names=\{([^}]+)',mods).group(1)+']');effects=json.loads('['+re.search(r'Effects=\{([^}]+)',mods).group(1)+']')
bindings=read('Assets/Scripts/GameBindings.cs');actions=json.loads('['+re.search(r'Actions=\{([^}]+)',bindings).group(1)+']');keys=re.findall(r'KeyCode\.(\w+)',re.search(r'Defaults=\{([^}]+)',bindings).group(1))
fauna=[]
for p in sorted((root/'Assets/Resources').glob('*.prefab')):
 s=p.read_text(encoding='utf-8-sig');m=re.search(r'\n  species: (.+)',s)
 if m:
  d={'resource':p.stem,'species':m.group(1).strip()}
  for key in ['kind','maxHealth']:
   v=re.search(r'\n  '+key+r': (\d+)',s)
   if v:d[key]=int(v.group(1))
  fauna.append(d)
book=json.loads(read('handbook-data.json'))
data={'baseline':'Unity 0.9.1 unreleased checkpoint; user stopped Unity verification to begin Unreal migration','date':'2026-10-27','units':'Unity metres/seconds; Unreal uses centimetres and seconds','weapons':weapons,'startingKits':kits,'attachments':[{'id':i,'name':names[i],'effect':effects[i],'slot':'Optic' if i<=3 else 'Barrel' if i<=6 else 'Handling'} for i in range(1,len(names))],'bindings':[{'action':a,'unityKeyCode':k} for a,k in zip(actions,keys)],'animals':fauna,'locations':book.get('locations',[]),'storm':{'fullSizeMetres':6144,'arenaSizesMetres':[2048,4096,6144],'participants':[{'min':1,'max':5,'tier':0},{'min':6,'max':12,'tier':1},{'min':13,'max':20,'tier':2}],'radiiMetresAtFullSize':[3040,2200,1520,938,500,200,0],'waitSeconds':[90,60,55,45,40,30,20],'closeSeconds':[100,140,125,110,85,60,45],'damagePerSecondByStage':[2,4,6,8,10,12,14],'radiusScale':'arenaSize/6144','initialRadius':'arenaSize * 0.704'},'source':'Definitions extracted by tools/export_legacy_rules.py; behavioral catalog links the implementation.'}
(out/'balance.json').write_text(json.dumps(data,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
sources=[]
for folder in ['Assets/Scripts','Assets/Editor']:
 for p in sorted((root/folder).glob('*.cs')):sources.append({'file':p.relative_to(root).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'lines':len(p.read_text(encoding='utf-8-sig').splitlines())})
(out/'source-inventory.json').write_text(json.dumps({'baseline':'Unity 0.9.1 checkpoint','files':sources},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'weapons':len(weapons),'kits':len(kits),'mods':len(names)-1,'controls':len(actions),'animals':len(fauna),'locations':len(book.get('locations',[])),'sourceFiles':len(sources)}))

rules=[]
for line in read('Design/rules-source.tsv').splitlines()[1:]:
 if not line.strip():continue
 category,rid,title,description,files=line.split('|')
 evidence=[]
 for filename in files.split(','):
  path=root/'Assets/Scripts'/filename
  if not path.exists():
   matches=[p for p in (root/'Assets/Scripts').glob('*.cs') if re.search(r'\bclass\s+'+re.escape(Path(filename).stem)+r'\b',p.read_text(encoding='utf-8-sig'))]
   if len(matches)!=1:raise ValueError('Unresolved rule source: '+filename)
   path=matches[0]
  evidence.append(path.relative_to(root).as_posix())
 rules.append(dict(id=rid,category=category,title=title,rule=description,sources=evidence,unityStatus='requested-not-implemented' if rid.startswith('GAP') else 'implemented-with-known-limits',unrealStatus='planned'))
(root/'Design/rules.json').write_text(json.dumps(dict(baseline='Unity 0.9.1 frozen checkpoint',date='2026-10-27',rules=rules),indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
lines=['# Pinefall rules and migration ledger','','Frozen when the user stopped Unity work on October 27, 2026. This inventories behavior, user overrides and outstanding requests. It does **not** claim Unreal already implements these features. Later user decisions supersede the ledger.','','Exact tables: [balance](Legacy/balance.json). Machine-readable [ledger](rules.json). Hashed [source inventory](Legacy/source-inventory.json). Regenerate with `python tools/export_legacy_rules.py`. Starting-kit fields omitted from C# default to zero.','','Each rule ID needs an Unreal owner, data mapping, implementation and acceptance scene. Preserve intent and balance; redesign legacy technology and bugs. Unity metres become Unreal centimetres. The first slice is deliberately smaller than the complete reserve.']
category=None
for rule in rules:
 if rule['category']!=category:category=rule['category'];lines+=['','## '+category]
 lines+=['','### '+rule['id']+' — '+rule['title'],'',rule['rule'],'','Unity evidence: '+', '.join('['+Path(p).name+'](../'+p+')' for p in rule['sources'])+'. Unreal: **planned**, not ported by this inventory.']
lines+=['','## Weapon table','','| Weapon | Class | Magazine | Player / animal damage | Interval | Reload | Range |','| --- | --- | ---: | ---: | ---: | ---: | ---: |']
for w in weapons:lines.append(f"| {w['name']} | {w['category']} | {w['magazine']} | {w['playerDamage']} / {w['damage']} | {w['interval']} s | {w['reload']} s | {w['range']} m |")
lines+=['','All eight kits, nine mods, forty bindings, sixteen animal prefabs and sixty named sites are in the exported tables. This catalog covers player-facing rules and engineering invariants. Hashed implementation remains available for deeper edge cases; legacy bugs are not desirable rules and requested features are not silently treated as completed.']
(root/'Design/RULES-CATALOG.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('Cataloged',len(rules),'code-linked rules.')
