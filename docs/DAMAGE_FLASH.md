# Damage flash

Dice Demon, Skeleton, Zombie, Slime, and the Knight player already call their controller's flash coroutine when health is reduced. Their effective prefab flash duration is 0.05 seconds; Zombie inherits its settings from Skeleton.

Dice Demon's palette material uses opaque white. The others share `Assets/Materials/PlayerFlashMaterial.mat`, now configured with the same opaque white `_FlashColor`. The previous red color's alpha was 0.137, limiting its flash to about 14% strength. Their normal appearance and flash timing are unchanged.

`Tools/Unity/MatchDamageFlash.cs` reapplies the reference color through Unity APIs after checking effective prefab material assignments and timing. Play mode testing is left to the user.
