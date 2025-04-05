
# PriceUpgradeScalingMod (Zmazor Mod)

> 🎮 A BepInEx mod for Unity-based multiplayer games that synchronizes upgrade prices and balances progression for all players.

## ✨ Features

- ✅ Upgrade prices scale based on the number of players.
- ✅ Upgrades are shared between all players (everyone has the best upgrades from the team).
- ✅ Simple configuration via `BepInEx/config/PriceUpgradeScaling.cfg`.
- ✅ Works with Photon (PUN) multiplayer.

## ⚙️ Formula

Upgrade cost is calculated as:

```
price = base + (players - 1) * scale
```

Example: With `scale = 0.8` and 3 players:
```
price = base * (1 + (3 - 1) * 0.8) = base * 2.6
```

## 🧩 Dependencies

- [BepInEx](https://github.com/BepInEx/BepInEx)
- [0Harmony](https://github.com/pardeike/Harmony)
- Unity (CoreModule, Input, etc.)
- PhotonUnityNetworking
- Assembly-CSharp from the game you're modding

## 🔧 Installation

1. Build the project in Visual Studio (use .NET Standard 2.1).
2. Copy the resulting `.dll` (e.g. `PriceUpgradeScaling.dll`) to:
   ```
   YourGameFolder/BepInEx/plugins/
   ```
3. (Optional) Adjust the `PriceScaling` value in:
   ```
   BepInEx/config/PriceUpgradeScaling.cfg
   ```

## 🧪 Usage

- Host a multiplayer game.
- Buy any upgrades.
- All players will automatically receive the same upgrades as the strongest member.

## 📜 License

MIT – Do what you want. Credit appreciated but not required.

---

Made with ❤️ by **Zmazor**
