# Enemy spawning

MainScene uses `Assets/Prefabs/Enemies/EnemySpawner.prefab` instead of individually placed enemies. The prefab defaults to 2–3 enemies ahead of the player, drawn from the slime, skeleton, and DiceDemon pool; scene instances can override the range in the Inspector. Shops, treasure rooms, and random rooms do not spawn this combat lineup. Opening MainScene directly also spawns a lineup for testing.

Select **Enemy Spawner** in the scene to edit the enemy pool, minimum/maximum count, spacing, and ground layer. Its position marks the first enemy's position; the rest appear to its right and align with the ground. Keep the spawner ID and each pool entry's ID unchanged once saves use them. Each spawner in a scene needs a unique ID.

All enemy types roll the same number of dice currently available to the player, including dice gained after the enemies spawned. The enemy controller's Inspector dice count is only a fallback when no player combat controller is available. Dice faces still roll randomly.

The slime has idle, movement, attack, hurt, and death animations, 40 HP, and 5 base attack damage. Skeletons retain their existing health, damage, and attack behavior.

DiceDemon uses the cacodaemon sheet for flight, attack, hurt, and death animations. Its prefab has 60 HP (three full hearts) and 10 base attack damage. **Enemy Flight** controls its height above the grounded spawn position (0.25), bob amplitude (0.08), and full cycle duration (2.4 seconds). It uses a kinematic body with no gravity, keeps bobbing during dice selection, and pauses with the game. On defeat it drops to the grounded spawn height over 0.3 seconds, then plays its death animation. Its collider and hearts follow the flying body. Saves retain the hover center and phase so repeated loads do not change its altitude.

**Enemy Projectile Attack** launches the animated `DiceDemonFireball.prefab` from the Mouth child under Visuals, after a short wind-up. The fireball takes 0.55 seconds to reach the player's collider, then applies the turn's damage and dice multiplier once. A blocked attack still shows the fireball and deals no damage. Combat waits for the shot to finish before continuing. The shared `EnemyController` keeps melee attacks for enemies without this component.

`DiceDemon Palette.mat` applies a muted, cooler red palette and a dark pixel outline across every animation. It also supports the existing white damage flash. Edit this material to tune the appearance without redrawing animation frames or changing the external source sheet.

Skeletons choose a random weapon when spawned. The **Enemy Weapon Variation** component on `Assets/Prefabs/Enemies/Skeleton.prefab` contains 19 SPUM swords, axes, daggers, a hammer, and a mace. Spears, bows, and staffs are excluded. Sword_5, New_Weapon_01, and New_Weapon_18 have also been removed from the pool. Older saves containing these retired weapons use the first allowed weapon instead. This changes appearance only; weapon choice does not change damage or attack timing. The sprites use the existing animated weapon hand, with no changes to the SPUM source assets.

Room saves preserve the exact enemy lineup, positions, health, defeated state, and selected weapons. An empty saved room stays empty. Older saves recreate their existing skeletons with the original sword. Weapon sprite names identify saved weapons, so keep those names stable and unique in the weapon pool.
