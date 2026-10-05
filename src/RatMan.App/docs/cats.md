# RatMan cats

## Identities and targets

The names and colors below are intentional: Pinky is cyan and Inky is pink.

| Cat | Color | Chase target | Scatter corner | Release |
| --- | --- | --- | --- | --- |
| Blinky | Red | Ratman's committed tile | Upper-right | Immediately |
| Pinky | Cyan | Two tiles ahead of Ratman's facing direction | Upper-left | 10% |
| Inky | Pink | `2 × Pinky's target − Blinky's committed tile` | Lower-right | 25% |
| Clyde | Orange | Ratman's tile at distance ≥ 8; otherwise his scatter corner | Lower-left | 50% |

Pinky preserves the upward targeting quirk: facing up targets two tiles up **and two tiles left**. Clyde uses Euclidean distance, without shortening it across tunnels.

Releases use the initial count of reachable cheese pieces, excluding empty tiles and the player spawn. Every collected piece counts once, including steroid cheese. Thresholds round upward. Cat bonus points never affect release progress. All four cats share the map's existing monster spawn; waiting cats are harmless and shown in the sidebar.

Cats choose shortest paths through the maze, including tunnels. Targets outside the board or outside their connected component resolve to the nearest reachable tile by squared distance, with row/column tie-breaking. Equal routes prefer Up, Left, Down, Right. Cats avoid reversing unless at a dead end or returning home, and continue moving when reaching a corner target.

## Steroid cheese and scatter

Cats chase by default. Eating steroid cheese gives Ratman a powered sprite and starts **10 seconds of scatter**. Active cats use their scared sprites, head toward their respective corners, and become edible. Another steroid resets the timer to 10 seconds. Newly released cats inherit the active mode.

Eating a scared cat awards **20 points** and sends it home harmlessly. Returning cats use faded scared sprites and cannot damage Ratman or award further points. At home they wait until the steroid effect ends. If the timer expires during their return trip, they resume chasing on arrival. Additional steroid cheese also extends the wait for cats already recovering.

| Actor/mode | Time per tile |
| --- | --- |
| Ratman | 350 ms |
| Chasing cat | 400 ms |
| Scared cat | 600 ms |
| Returning cat | 200 ms |

Route and speed changes take effect at the next tile boundary; vulnerability and appearance change immediately. There is no periodic chase/scatter schedule.

## Lives, victory, and replay

Start with **three lives**. Contact with a chasing cat removes one life. After a one-second pause, actors return to their spawn points, Ratman is idle, pending input and power are cleared, and cats eligible at the retained release progress leave home again. Score and consumed cheese survive life loss. Several cats touching Ratman together still cost only one life.

The third catch shows Game Over. Collecting the final cheese shows victory. Both outcomes freeze gameplay and offer Replay and Return to Menu. Replay restores the same map's food, score, lives, cats, and timers.

Food is collected on actual tile arrival. Steroid activation precedes contact at the same instant, and last-cheese victory precedes a simultaneous catch. Interior head-on crossings, overtakes, stationary actors, and tunnel crossings also count as contact; merely reserving a destination does not.

An empty-food map wins on its first simulation tick. Disconnected maps remain playable with a warning if the cat home cannot reach Ratman. Cats stay within their own connected component.

## Engine and presentation

`RatMan.Game` owns movement, routing, releases, power timing, collisions, score, and round outcomes. `GameSession.RequestDirection` queues input; `Advance` consumes elapsed time in 10 ms steps and retains fractional time. Actor positions commit at arrival, independently of their current destination and movement progress. Speeds and durations are immutable session rules.

WPF projects this state and animates the provided sprites. The eight cat sheets and powered Ratman sheet use measured frame rectangles with a shared transparent canvas to avoid clipping, stretching, or inconsistent frame scale. Waiting/recovering cats appear in the HUD; active/returning cats appear on the board. The existing map format and single monster-spawn editor tool are unchanged.
