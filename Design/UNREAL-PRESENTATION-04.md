# Pinefall presentation pass — 0.4

## HUD direction

Use the useful hierarchy of Fortnite and Apex: a clear center, self and squad vitals at lower left, weapons/ammunition at lower right, and map/round information at upper right. Use original Pinefall art and icon outlines; no imported branding or copied UI artwork.

Tokens: deep navy translucent surfaces; off-white text; azure shields; mint health; amber hunger; violet storm. Barlow Condensed SemiBold is the display face; body labels use the existing readable sans. Large ammo figures, restrained uppercase labels, broad health bars, compact squad cards, and rarity edge colors replace paragraphs of status text. Icons supplement names rather than replacing necessary instructions.

At 1280×720 the corners must remain separated, item labels wrap, and inventory must scroll if needed. The inventory remains at right with the world visible. Only implemented controls are displayed. Team information must not reveal enemies; unseen teammates need owner-only server summaries rather than relying on nearby replicated pawns.

## Environment direction

Use licensed scanned mature vegetation and rock faces at plausible scales. Woodland, wet jungle, alpine rock, arid scrub, snow country and meadow receive different species/ground-cover mixes. Low grass keeps small wildlife visible. Village clearances should keep paths clear without removing every blade of vegetation around an entire settlement.

Use Nanite for appropriate dense static geometry, not as a substitute for source quality. Keep grass distance-limited and shadow-free, preserve collision independently of visual distance, and retain supported non-Nanite fallback rendering. Surface normals/roughness and macro variation matter as much as raw mesh density.

## Review before implementation

The previous HUD has oversized branding, duplicated health/ammo text, a very small weapon readout and a text-only squad list. The current forest stretches a sapling several times beyond its natural size; settlements have an overly broad vegetation exclusion. Flat vehicle materials and repeated cabin geometry remain visible limitations and need honest asset work, not merely higher rendering settings.

References examined: Fortnite Battle Royale screenshots (Windows Central map update/Chug Jug coverage), Apex launch gameplay screenshots (Digitec and Forbes), and the user's supplied references. UE 5.6 Nanite documentation and Poly Haven's CC0 license informed asset preparation.
