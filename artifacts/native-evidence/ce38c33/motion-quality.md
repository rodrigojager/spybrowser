# Motion quality benchmark result

JSON: `motion-quality.json`

Machine: `DESKTOP-KLQV3JP`; OS: `Microsoft Windows 10.0.19045`; .NET: `.NET 8.0.22`; browser: `149.0.7827.55`; seed: `21021`.

| Algorithm | Case | DOM moves | planned ms | wall ms | path px | displacement px | interval p95 ms | velocity p95 px/s | accel p95 px/s² | pauses >=50ms | complete | chronological |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|:---:|:---:|
| Bezier | short-subpixel p0 | 25 | 300 | 456,9 | 1,0 | 1,0 | 29,0 | 0,0 | 0,0 | 0 | True | True |
| Bezier | long p0 | 25 | 300 | 448,7 | 1155,4 | 1153,3 | 28,2 | 4838,5 | 79144,1 | 0 | True | True |
| Bezier | stable-button-activation p0 | 27 | 300 | 494,6 | 114,9 | 114,1 | 36,7 | 450,7 | 8540,3 | 1 | True | True |
| Bezier | slow-transport-acknowledged-12ms-before-move p0 | 25 | 300 | 1037,6 | 643,9 | 642,7 | 51,6 | 1227,6 | 11259,3 | 5 | True | True |
| Bezier | concurrent-independent-pages p0 | 25 | 300 | 460,1 | 643,9 | 642,7 | 29,4 | 2938,4 | 45149,5 | 0 | True | True |
| Bezier | concurrent-independent-pages p1 | 25 | 300 | 474,5 | 685,5 | 684,1 | 36,2 | 2721,9 | 37913,1 | 0 | True | True |
| Bezier | concurrent-independent-pages p2 | 25 | 300 | 446,8 | 728,3 | 726,8 | 28,4 | 2809,0 | 49869,9 | 0 | True | True |
| Cursory | short-subpixel p0 | 3 | 300 | 323,8 | 1,0 | 1,0 | 157,3 | 6,4 | 41,2 | 2 | True | True |
| Cursory | long p0 | 21 | 300 | 347,0 | 1195,7 | 1158,6 | 18,0 | 11400,4 | 725806,4 | 0 | True | True |
| Cursory | stable-button-activation p0 | 22 | 300 | 440,1 | 121,4 | 114,1 | 38,7 | 945,3 | 21067,4 | 1 | True | True |
| Cursory | slow-transport-acknowledged-12ms-before-move p0 | 19 | 300 | 336,6 | 669,0 | 644,5 | 29,4 | 18304,5 | 995817,8 | 0 | True | True |
| Cursory | concurrent-independent-pages p0 | 20 | 300 | 333,5 | 668,8 | 644,5 | 18,2 | 13251,6 | 560202,7 | 0 | True | True |
| Cursory | concurrent-independent-pages p1 | 17 | 300 | 332,6 | 699,9 | 687,2 | 46,2 | 7926,9 | 353092,7 | 0 | True | True |
| Cursory | concurrent-independent-pages p2 | 20 | 300 | 332,5 | 750,8 | 729,9 | 18,2 | 8605,0 | 307001,9 | 0 | True | True |

Cursory cold generation/load: 285,2 ms; warm p50/p95: 10,1/33,9 ms; allocated: 2943937 bytes/trajectory; same-seed determinism (1e-9): True; initial 10 ms p95 target met: False.

DOM `performance.now()` timings include actual browser scheduling. The slow case includes artificial 12 ms delay before each real mouse dispatch, not network latency. Results describe implementation cost and observed rhythm only; they do not claim improved humanness, stealth, CAPTCHA outcomes, or detection avoidance.
