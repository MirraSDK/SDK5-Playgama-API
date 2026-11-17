# Playgama API for MirraSDK5

MirraSDK5 providers that run a game on Playgama and its partner platforms through Playgama Bridge for Unity.

Requires Unity 2022.3 or newer, MirraSDK5 5.2.3 or newer and Playgama Bridge for Unity 2.4.0 or newer.

## Setup

1. Add Playgama Bridge for Unity in Package Manager by git URL, pinned to a release tag:

    ```
    https://github.com/playgama/bridge-unity.git#v2.4.0
    ```

2. Add this package in Package Manager by git URL:

    ```
    https://github.com/MirraSDK/SDK5-Playgama-API.git
    ```

3. Run `Playgama > Install Template Files`. Select the `Bridge` template in `Player Settings > Resolution and Presentation`, and keep `Decompression Fallback` on.

4. In `Assets/WebGLTemplates/Bridge/playgama-bridge-config.json`, list every product id under `payments`. Set the prices in the Playgama developer dashboard.

5. In `MirraSDK Toolkit`, select `PlaygamaConfiguration` as the build configuration and set `PlaygamaPayments > ConsumableProductTags`: the comma separated product tags that can be bought again. Every other product is permanent.

## Behaviour

- **Data.** One storage key, `json-data`.
  - Writes wait until the save has been read.
  - Only one write is in flight at a time, and it always carries the newest data.
  - A save that cannot be read after retries leaves saving off for the session, so it is never overwritten.
- **Payments.** Bridge payments calls run one at a time.
  - A consumable is consumed once the save holding the granted product is stored.
  - `RestorePurchases` returns:
    - consumables that were paid for but never granted
    - permanent products not granted on this save yet

  Call it on every launch and grant each pending product with `RestoreProduct`.
- **Ads.** An ad counts as visible from the request until it closes, so a second request meanwhile is refused. A rewarded ad succeeds only after the `Rewarded` state.
- **Pause and audio.**
  - The game pauses on the platform pause state, on a lost focus and during ads.
  - A focus lost to an ad is released when the ad closes.
  - Audio starts muted when the platform has sound off.
- **Share.** Shares the `social.shares` entry set in `PlaygamaPlatformInteractions > ShareConfigId` (default `default`).
- **Leaderboards.** Read only when the platform has in-game leaderboards. On `playgama` itself this needs Bridge SaaS leaderboards.
