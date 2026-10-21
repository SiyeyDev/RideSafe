# RideSafe VR — Game Design Document (BETA FINAL v0.9)

> Text extracted with pdftotext from `Docs/design/RideSafe_UX_UI_v3.zip` → `uploads/RideSafe VR GDD VF1.pdf` on 2026-10-16. Source of truth is the PDF. Local only (gitignored).

```text
EXPERIENTIAL MICRO-LEARNING + INDEPENDENT TRANSFER ASSESSMENT

RideSafe VR

Game Design Document

A complete board and production reference for a continuous Quest 3 safety experience. The
design teaches through embodied practice, repairs the primary causal error quickly and
measures independent application in five changed scenes.

BETA FINAL v0.9  FIGMA VERSION 11  5 MODULES  ROAD READY / NOT YET ROAD READY

Primary vehicles
E-bike (seated) and e-scooter (standing). E-moped only through an approved vehicle/rule profile.
Bicycles remain a compatible secondary profile.

Prepared for RideSafe board, product, learning design, XR engineering, art/audio, QA,
assessment and governance.

August 3, 2026
RIDESAFE VR - GAME DESIGN DOCUMENT                 BETA FINAL v0.9

Contents                                                          3
                                                                  4
Product and Scope                                                 5
Learner Journey and Time Budget                                   7
Learning and Repair Architecture                                  8
Five Learning Modules                                            10
Module 01 - Ready to Ride                                        12
Module 02 - Intersections & Crossings                            14
Module 03 - Hazard Anticipation                                  16
Module 04 - Buses, Trucks & Curbside                             18
Module 05 - Attention & Shared Space                             19
Rider UX and Accessibility                                       20
Assessment, Session Telemetry and Privacy                        22
World, Art and Technical                                         24
Production Roadmap and Governance
Open Decisions and Production Backlog

Board and production reference - Figma Version 11  Page 2
RIDESAFE VR - GAME DESIGN DOCUMENT                                 BETA FINAL v0.9

Product and Scope

5                 16+                              about 15:30     about 21:30

learning modules  initial audience                 returning base  maximum with capped
                                                                   repair

What the product is

A practice-first VR experience for riders and prospective riders age 16+. It builds readiness, perception, priority, speed,
position, clearance and attention decisions, then requires each skill to be demonstrated independently in a changed scene.

Where it applies

Neighborhoods, suburban and urban streets, schools, corporate, institutional and commercial settings. Campus is an optional
context skin, not the baseline world model.

Outcome and boundary

The learner must recognize risk early, create a safe margin, choose correct priority and timing, and pass all five changed
transfers. The overall result is Road Ready only when every module passes. RideSafe is not a legal license, government
certification, insurance decision, medical detector, employee-surveillance tool or guarantee of real-world safety.

Board and production reference - Figma Version 11                  Page 3
RIDESAFE VR - GAME DESIGN DOCUMENT                                                                          BETA FINAL v0.9

Learner Journey and Time Budget

Stage                 Purpose                                                                   First-time  Returning
                                                                                                            45-60 sec
Outside VR fit/admin  Vehicle, accessibility, headset/handlebar fit, hygiene and safe area      45-60 sec   45-60 sec

Welcome, vehicle and VR Comfort, head scan, controls, pause/recenter, control check, low-speed 1:30-2:00    2:00
                                                                                                            3:00
orientation           start/turn/both-brake stop; hold both brakes to begin                                 3:00
                                                                                                            3:00
Module 1              Readiness and go/no-go                                                    2:00        3:00
                                                                                                            30-45 sec
Module 2              Intersection, crossing and speed judgment                                 3:00

Module 3              Hazard anticipation, wet surfaces and margin                              3:00

Module 4              Large vehicles, waiting, speed and clearance                              3:00

Module 5              Attention, fatigue and shared-space behavior                              3:00

Results/reset         Five transfer statuses, binary overall result, manual external entry and  30-45 sec
                      clear session

   Timing rule: Each failed module may add one repair of no more than 60 seconds. Each module owns its changed
   transfer; there is no separate final transfer route.

First-time VR orientation

 - Admin selects e-bike seated or e-scooter standing; approved e-moped only when available. Vehicle remains locked for the
    session.

 - Stationary training bay teaches comfort, head scanning, controller mapping, pause and recenter.
 - Control verification is followed by low-speed start, steering and progressive both-brake stop.
 - The learner holds both brakes to confirm readiness and begin.

Board and production reference - Figma Version 11                                                                      Page 4
RIDESAFE VR - GAME DESIGN DOCUMENT                                                                    BETA FINAL v0.9

Learning and Repair Architecture

1. First attempt       2. Freeze Primary           3. What / why / next  4. Coached micro-retry  5. Changed transfer
Unassisted diagnostic  causal error                One actionable        Nearest decision gate   Unguided and scored
                                                   message

Decision gate and restart

Restore a deterministic scene state 5-8 seconds before the safety decision. This is separate from a technical recovery
checkpoint. Pause for spatial orientation, verify both brakes, then use a three-second countdown.

Progressive cue ladder

Begin with a general cue, then a spatial cue, then an explicit action. Use fully guided support only if needed. One repair is
allowed, capped at 60 seconds. If still unsuccessful, show one brief correct demonstration and continue.

Reusable error families

 - Readiness
 - Scan/perception
 - Priority/yield
 - Speed/timing
 - Position/clearance
 - Braking/control
 - Distraction/context

Feedback asset tiers

Tier  Asset                                                 Use
                                                            Reusable common-error feedback
L1    Text, audio line, icon and highlight                  Spatial explanation of risk and margin

L2    Paused-scene path, blind zone, stopping or clearance  Complex procedure or unsuccessful repair

      overlay

L3    Short authored correct demonstration

Learning rationale: Active attempt, immediate actionable feedback, fading prompts, embodied retrieval and
independent changed transfer. Evidence references include Shute (2008), Hattie and Timperley (2007), Renkl/Atkinson
worked-example fading, the 2024 VR safety-training meta-review and W3C XR Accessibility User Requirements.

Cross-Cutting Speed System

   Speed is visible and assessable throughout every moving module. A compact persistent display shows current
   speed. Posted regulatory signs and differentiated advisory signs appear before relevant decision gates. The admin
   locale selects mph or km/h; telemetry stores a neutral canonical value and renders the selected unit.

Board and production reference - Figma Version 11                                                                       Page 5
RIDESAFE VR - GAME DESIGN DOCUMENT                     BETA FINAL v0.9

Module Speed presentation                              How it is used
                                                       No riding-speed grade
M1  Stationary; unit/profile confirmed in orientation  Limit compliance and lower safe approach speed graded in transfer
                                                       Early reduction, following distance and stopping margin graded
M2  Posted road limit plus crossing context            Lower conflict speed and controlled overtaking graded
                                                       Speed near users, braking onset and relative passing speed graded
M3  Posted limit plus occlusion/wet cues

M4  Posted corridor limit plus relative passing speed

M5  Posted greenway/shared-path advisory speed

Interpretation: A displayed limit is never a target or proof of safety. Rain, occlusion, crossings, people, large vehicles
and limited clearance require lower speeds. Thresholds, stopping envelopes and posted/advisory values are versioned
per vehicle and jurisdiction, validated in the graybox and displayed in mph or km/h.

Board and production reference - Figma Version 11      Page 6
RIDESAFE VR - GAME DESIGN DOCUMENT                                                                      BETA FINAL v0.9

Five Learning Modules

Structural rule: Every module contains one continuous first-attempt scenario, one bounded repair and one continuous
changed-transfer scenario. A scenario may contain multiple linked decision moments required to teach the full objective.

Module                Duratio  Learning outcome                            Scored evidence
                      n

01 - Ready to Ride    2:00     Select every required safety item and       One hundred percent completion in the changed
                               complete every essential pre-departure      scene: every required item and applicable component
                               check.                                      identified, every distraction rejected or corrected, no
                                                                           incorrect selection, no critical omission and the correct
                                                                           independent go / no-go decision.

02 - Intersections &  3:00     Approach every crossing at a legal and      In the changed scene, complies with the displayed
                               condition-appropriate speed, visible and    limit, selects a lower safe approach speed, stops/waits
Crossings                      ready to yield.                             as required, scans both directions after the signal
                                                                           changes and crosses independently with no conflict or
                                                                           prompt.

03 - Hazard           3:00     Read developing hazards early enough In the changed scene, independently recognizes rain
Anticipation
                               to reduce speed and create space            and surface cues, reduces speed early, maintains

                               before action is urgent.                    following distance, selects a safe line and completes a

                                                                           smooth stop with clearance and no abrupt low-traction

                                                                           input, near miss or prompt.

04 - Buses, Trucks &  3:00     Wait outside blind and swept paths;         In the changed scene, remains outside the swept
                               overtake only when visibility, time, speed  path, respects the displayed speed while slowing for
Curbside                       and clearance are safe.                     conditions, and independently waits or passes only
                                                                           with verified visibility, timing, relative speed and
                                                                           clearance.

05 - Attention & Shared 3:00   Protect attention and use a shared path     In the changed scene, independently controls speed,
Space                          at a speed that preserves time,             avoids the encroaching user through a safe defensive
                               communication and clearance.                response, communicates without forcing priority,
                                                                           preserves clearance, then recognizes the authored
                                                                           fatigue cue and completes a safe pull-off and stop
                                                                           without prompt or conflict.

Board and production reference - Figma Version 11                                                                            Page 7
RIDESAFE VR - GAME DESIGN DOCUMENT                                                             BETA FINAL v0.9

Module 01 - Ready to Ride

Duration  Safety promise

2:00      Select every required safety item and complete every essential pre-departure check.

Teach

 - Helmet fit and fastening.
 - Working white front light, red rear light and lateral reflectors.
 - Closed, secure footwear; secured clothing, straps and belongings.
 - Phone stored away with notifications off; no handheld use.
 - No music or headphones while moving, or audio low enough to preserve full environmental awareness.
 - Tire pressure and visible condition; front and rear brakes.
 - Frame and primary structure; secure handlebar/stem, seat/seatpost, wheels/axles and any folding latch.
 - Throttle return to neutral; battery securely attached with no visible damage.
 - Lights and reflectors functional; cargo or accessories checked only when present.

First attempt - one continuous scenario

One stationary departure scene with two required stations. At the equipment station, the learner selects every safe item and
rejects each distraction from a broader object set. At the vehicle station, the learner identifies every universal inspection point
and every applicable component visible on the selected e-bike or e-scooter, then makes the final go / no-go decision. The
vehicle is correctly functioning so the task measures knowledge of the complete procedure rather than the ability to see
subtle defects.

Decision gate

The learner remains at the building exit before entering the travel path. Submission is locked until the learner deliberately
confirms the equipment set, inspection set and go / no-go decision.

Feedback and repair

Show the exact required item, distraction or vehicle component that was missed or incorrectly selected. Explain what it is,
why it matters and the required action. One coached retry repeats the same two-station procedure from the departure gate.

Changed transfer - one continuous scenario

One changed departure scene uses a rearranged equipment set and a different approved vehicle presentation. The learner
must again select all required equipment, reject every distraction, identify all universal and present conditional inspection
points, and make the independent go / no-go decision.

   Critical rule: Do not depart until personal equipment, attention and every safety-critical vehicle check are ready. Quest
   does not diagnose actual fatigue, intoxication or impairment.

Board and production reference - Figma Version 11                                              Page 8
RIDESAFE VR - GAME DESIGN DOCUMENT                 BETA FINAL v0.9

Observable telemetry

Initial equipment selection set and order; required/unsafe choices; omissions/false selections; inspection points selected and
order; applicable conditional checks; final go/no-go; premature departure; completion time.

   Pass evidence: One hundred percent completion in the changed scene: every required item and applicable component
   identified, every distraction rejected or corrected, no incorrect selection, no critical omission and the correct independent
   go / no-go decision.

Board and production reference - Figma Version 11  Page 9
RIDESAFE VR - GAME DESIGN DOCUMENT                 BETA FINAL v0.9

Module 02 - Intersections & Crossings

Duration  Safety promise

3:00      Approach every crossing at a legal and condition-appropriate speed, visible and ready to yield.

Teach

 - Read posted road-speed signs and the persistent speed display.
 - A posted limit is a ceiling, not a target when approaching a conflict.
 - Signal recognition and right-turning vehicle cues.
 - Early scan before the stop line and visible road position.
 - Pedestrian priority and stopping before the crossing.
 - Reaction time and the sharp increase in stopping distance as speed rises.
 - Green permits movement only after the full path is checked.

First attempt - one continuous scenario

One continuous signalized-intersection scene integrates two decision moments. The learner approaches within the posted
limit but must reduce further for the crossing, detects a vehicle preparing a right hook, and yields outside its turning path. At
the same intersection, a pedestrian enters the marked crossing and the learner must stop before the line, wait and proceed
only after the pedestrian and vehicle paths are clear.

Decision gate

The gate begins 5-8 seconds upstream with the posted speed sign, signal state, turning vehicle and pedestrian area visible.
Correct preparation must make a controlled stop possible without emergency braking.

Feedback and repair

Identify the primary causal error: ignored sign, unsafe condition-adjusted speed, missed turn cue, invisible position, late
braking or failure to yield. Show the conflict path and stopping envelope, then repeat the approach once with progressive
cues.

Changed transfer - one continuous scenario

One changed path-to-road crossing scene uses different geometry and speed signage. The learner waits for the permitted
signal, then independently scans left, right and left again. A late or turning vehicle may require continued waiting even after
green. Proceeding is correct only when the complete crossing is clear.

   Critical rule: A signal or posted limit never guarantees a clear path. Control speed early, scan both directions and yield
   whenever the crossing is not demonstrably clear.

Observable telemetry

Posted/advisory speed observed; speed at gate and crossing; time/distance above limit or safe approach threshold;
head-orientation scan sequence; braking onset; stop position; wait time; signal/yield decision; path conflict; stopping margin.

Board and production reference - Figma Version 11                                                          Page 10
RIDESAFE VR - GAME DESIGN DOCUMENT                 BETA FINAL v0.9

Pass evidence: In the changed scene, complies with the displayed limit, selects a lower safe approach speed,
stops/waits as required, scans both directions after the signal changes and crosses independently with no conflict or
prompt.

Board and production reference - Figma Version 11  Page 11
RIDESAFE VR - GAME DESIGN DOCUMENT                 BETA FINAL v0.9

Module 03 - Hazard Anticipation

Duration  Safety promise

3:00      Read developing hazards early enough to reduce speed and create space before action is urgent.

Teach

 - Look -> Create space -> Act.
 - Read the posted limit while selecting a lower speed for occlusion or poor surface conditions.
 - Scan doors, occupants, wheels, balls and gaps.
 - Moderate speed before the hazard becomes visible.
 - Create lateral and following-distance margin.
 - Use smooth, progressive braking and steering.
 - In rain, avoid wet paint, metal, leaves, oil and standing water when safe; otherwise cross slowly, upright and without abrupt

    inputs.

First attempt - one continuous scenario

One continuous neighborhood corridor contains two predictive hazard moments: a parked-car door/occlusion risk followed by
a ball emerging before a child. The learner must use the posted speed as a ceiling, scan early, reduce speed before each
occlusion and create lateral or stopping margin before either hazard fully develops.

Decision gate

The gate begins before the first occlusion with the speed sign, parked vehicles, door zone and downstream gap readable.
The timing makes both hazards avoidable through upstream preparation rather than reflex.

Feedback and repair

Highlight the earliest missed cue, excessive speed, late braking or insufficient lateral margin. Visualize the predicted hazard
path and the speed/space envelope that would have prevented the conflict, then retry the continuous corridor once.

Changed transfer - one continuous scenario

One changed wet-weather scene presents a visible low-traction surface and a lead vehicle approaching a signal. The learner
must reduce below the posted limit for rain, increase following distance, choose a safe line around the slippery surface - or
cross it slowly and upright - and respond to the signal and brake lights early enough to stop progressively with clearance.

   Critical rule: Look early, reduce speed and create space before the hazard; on wet or low-traction surfaces, avoid
   abrupt braking or steering.

Observable telemetry

Posted speed; speed before occlusion/wet zone; time above condition-safe threshold; detection/scan proxy; lateral line;
following time headway; braking onset and progression; steering rate; braking/turning on low-traction surface; final clearance;
near miss.

Board and production reference - Figma Version 11  Page 12
RIDESAFE VR - GAME DESIGN DOCUMENT                 BETA FINAL v0.9

Pass evidence: In the changed scene, independently recognizes rain and surface cues, reduces speed early,
maintains following distance, selects a safe line and completes a smooth stop with clearance and no abrupt low-traction
input, near miss or prompt.

Board and production reference - Figma Version 11  Page 13
RIDESAFE VR - GAME DESIGN DOCUMENT                 BETA FINAL v0.9

Module 04 - Buses, Trucks & Curbside

Duration  Safety promise

3:00      Wait outside blind and swept paths; overtake only when visibility, time, speed and clearance are safe.

Teach

 - Blind zones and swept-turn paths.
 - Posted corridor speed and lower speed around large/loading vehicles.
 - Stop and wait behind with enough clearance to see the situation.
 - Check left/rear, oncoming flow, driver visibility and escape path.
 - Continued waiting is a complete and correct decision.
 - Pass only with a legal gap, low relative speed and adequate lateral clearance.

First attempt - one continuous scenario

One continuous large-vehicle and loading corridor contains two decision moments. A bus or truck begins a swept turn,
requiring the learner to hold back outside the blind/swept area. Later, a loading vehicle may be passed only after the learner
stops or waits behind with clearance, checks left/rear and oncoming traffic, confirms visibility and escape space, and controls
passing speed. The scenario never forces an overtake.

Decision gate

The learner starts behind the large vehicle with enough distance to see its signals, wheels, driver area, curb activity and
adjacent traffic. Posted speed is visible before the corridor, but the safe operating speed is lower near the conflicts.

Feedback and repair

Show the blind/swept overlay or the failed gap, scan, speed or clearance. State whether the safe action was to wait,
reposition or pass more slowly with greater space, then retry the same corridor once.

Changed transfer - one continuous scenario

One changed corridor uses a different large/loading vehicle, curb configuration, posted speed and oncoming-gap pattern.
The learner must independently hold back from the swept path and later choose continued waiting or a safe,
low-relative-speed pass with verified visibility and clearance.

   Critical rule: Never pass inside a turning large vehicle. If the driver, escape path, legal gap or adequate clearance
   cannot be confirmed, wait.

Observable telemetry

Posted speed; corridor/relative passing speed; blind-zone exposure proxy; following clearance; wait time; left/rear scan;
overtaking decision; oncoming gap; minimum lateral clearance; abrupt maneuver; near miss.

Board and production reference - Figma Version 11  Page 14
RIDESAFE VR - GAME DESIGN DOCUMENT                 BETA FINAL v0.9

Pass evidence: In the changed scene, remains outside the swept path, respects the displayed speed while slowing for
conditions, and independently waits or passes only with verified visibility, timing, relative speed and clearance.

Board and production reference - Figma Version 11  Page 15
RIDESAFE VR - GAME DESIGN DOCUMENT                 BETA FINAL v0.9

Module 05 - Attention & Shared Space

Duration  Safety promise

3:00      Protect attention and use a shared path at a speed that preserves time, communication and
          clearance.

Teach

 - Phone notifications are ignored while moving or handled with Y only after a complete safe stop.
 - Fatigue or drowsiness requires a safe pull-off and an end to that riding segment; Quest presents the cue but does not diagnose

    real fatigue.
 - Use the posted greenway/shared-path advisory speed as a ceiling and slow further near people.
 - Release throttle, brake early and wait behind pedestrians or slower users.
 - X bell is a courtesy alert and never grants priority.
 - Pass only with low relative speed, clear oncoming space and adequate lateral clearance.
 - When another rider encroaches, slow, keep/move right only if the edge is safe, communicate and stop rather than force a

    conflict.

First attempt - one continuous scenario

One continuous permitted greenway scene contains two linked decision moments. A phone notification appears while the
learner is moving; the learner must ignore it or stop fully in a safe area before using Y. Farther along, two pedestrians occupy
the path. The learner must obey the posted advisory speed, reduce speed early, wait, check oncoming space, use X as a
courtesy cue and pass left only at low relative speed with adequate clearance - or continue waiting.

Decision gate

The attention event occurs with a visible safe pull-off. The shared-path gate begins far enough behind the pedestrians for the
advisory sign, current speed, following distance and oncoming path to be evaluated without pressuring a pass.

Feedback and repair

For attention, show the head-down/off-path duration, distance traveled and any control change, then state ignore-or-stop. For
shared space, identify excessive speed, late braking, missing wait/alert, unsafe gap or insufficient clearance and rehearse the
full sequence once.

Changed transfer - one continuous scenario

One changed greenway scene reverses the order of risk. An oncoming cyclist, e-bike or scooter drifts into the learner's side;
the learner must slow, communicate, use the safe right-side space when available and stop if adequate separation cannot be
created. After the conflict clears, an authored fatigue/drowsiness cue appears near a safe pull-off; the learner must reduce
speed, stop safely and end the segment.

   Critical rule: If attention or alertness is compromised, remove the distraction or stop safely. In shared space, priority
   never justifies speed or movement that creates a preventable conflict.

Board and production reference - Figma Version 11                                                    Page 16
RIDESAFE VR - GAME DESIGN DOCUMENT                 BETA FINAL v0.9

Observable telemetry

Advisory speed observed; speed at gate/near users; time above posted or condition-safe threshold; notification choice; Y
moving/stopped; head yaw/pitch excursion; longest/cumulative off-path duration; distance traveled off-path; braking onset;
wait/bell timing; lateral response; relative passing speed; minimum clearance; fatigue stop response; near miss.

   Pass evidence: In the changed scene, independently controls speed, avoids the encroaching user through a safe
   defensive response, communicates without forcing priority, preserves clearance, then recognizes the authored fatigue
   cue and completes a safe pull-off and stop without prompt or conflict.

Board and production reference - Figma Version 11  Page 17
RIDESAFE VR - GAME DESIGN DOCUMENT                                                                      BETA FINAL v0.9

Rider UX and Accessibility

Prototype control map                              Function
                                                   Steering from two rigidly mounted controllers
  Input                                            Throttle
  Handlebar rotation                               Front brake prototype default; confirm per rig
  Right grip / middle-finger trigger               Rear brake prototype default; confirm per rig
  Right index trigger                              Bell / courtesy alert; gives no priority
  Left index trigger                               Context/phone interaction only while fully stopped
  X                                                Reserved; no vocal-emulation button in MVP
  Y                                                Observation/scan proxy only; never standalone proof
  Left grip
  Head orientation

Calibration and safe control

Record mounting axis/midpoint and neutral handlebar. Apply smoothing, dead zone, maximum steering, speed-sensitive gain
and tracking-confidence checks. If tracking degrades, reduce or stop motion and offer pause/recenter or an Incomplete
technical exit. Teach release throttle, progressive braking and both-brake controlled stops. Do not describe the front brake as
inherently dangerous. No pedaling input.

Admin lifecycle

Before handoff, the admin selects the vehicle, language and accessibility settings; fits the headset and handlebar; calibrates
and runs the control check. After results, the admin records the binary result externally, clears the local session and prepares
the next learner. A protected control ends operations.

Comfort and accessibility

 - First-time Quest tutorial; stable horizon; reduced motion; pause/recenter; comfort exit; seated/standing profile.
 - No forced camera pull, forced fall, flashing stimulus or graphic injury.
 - Separate environment, instruction, feedback and UI audio channels; independent volume and mono compatibility.
 - Subtitles for every spoken line; directional information repeated visually and textually.
 - Warnings combine color with shape, icon or text.
 - Decision-gate retry uses the paused 3D scene; technical recovery may show a static thumbnail.

Board and production reference - Figma Version 11                                                       Page 18
RIDESAFE VR - GAME DESIGN DOCUMENT                                               BETA FINAL v0.9

Assessment, Session Telemetry and Privacy

Stage/state           Scored? Stored during session                              Outcome
                                                                                 Proceed or repair
First attempt         No            Temporary error/control/perception evidence  Proceed to transfer; one repair
                                                                                 Independent module status
Coached repair        No            Temporary cue level and completion           Road Ready only if all five pass
                                                                                 Incomplete; not assessment failure
Changed transfer x 5  Yes           Temporary Pass / Not Passed

Overall               Binary        Five temporary module statuses

Comfort/technical/volunta No        Temporary reason category
ry exit

Ephemeral event contract

Scenario/content version, vehicle/access mode, decision event, speed, position, braking, clearance, time, near-miss/collision
flag, comfort/technical exit and head-orientation scan proxy. Process locally for the active session, then clear.

External result boundary

The end screen shows five temporary module statuses and the overall binary result. The admin manually enters only the
overall result into the separate online form/certificate workflow. There is no MVP API. The local session is cleared before the
next learner.

   Never capture in production: Name, email, student/employee ID, rider profile, pseudonym, Meta account ID, photo,
   voice recording, eye tracking, raw gaze, camera/passthrough recording, video replay or persistent history. No third-party
   analytics and no employee-discipline use. A separately consented research/development build may export diagnostic
   data; that is not production behavior.

Board and production reference - Figma Version 11                                                                    Page 19
RIDESAFE VR - GAME DESIGN DOCUMENT                   BETA FINAL v0.9

World, Art and Technical

Connected modular route

Approximately 1.2-1.6 km represented route using compact occlusion and transitions. Learning zones are roughly 150-300 m
each: building exit, neighborhood/local street, crossings, parked-car corridor, large-vehicle/loading corridor, wide shared path
and safe pull-offs.

Context and art direction

Residential, school/institutional, commercial and corporate facade kits support regular streets and sidewalks. Campus is
optional. The route begins at a clear building exit, not a mobility hub. Art is semi-realistic with readable silhouettes, safety
cues visible at decision distance, restrained HUD and non-graphic outcomes.

Planning asset inventory

Asset family                      Planning count     Coverage
Road/curb/sidewalk tile families  6-8 families; 2-4  Local street, crossing, loading curb, wide sidewalk/path and
                                  variants           connectors
Building facade kits              8-12               Residential, school/institutional, commercial, corporate and clear
                                                     exits
Intersection layouts              4                  Signal, stop and crossing configurations
Curb/loading layouts              3                  Loading van, bus stop and truck corridor
Shared paths                      2-3                Wide permitted geometries with safe passing space
Passenger vehicles                6-8                Color/state, door, turn and parked variants
Vans / buses / trucks / loading   2/2/3/2            Swept-path, blind-zone and loading states
Bicycle/e-bike/e-scooter actors   4-6                Slower and opposing-flow users
Pedestrian actors                 10-14              Child/ball, stroller, mobility aid and diverse adults
Street props                      25-40              Signs, lights, bollards, bins, racks, vegetation and occluders
Hazard/audio families             Per error family   Door, signal, path, overlay, notification, vehicle, bell, voice and UI
                                                     cues

Board and production reference - Figma Version 11    Page 20
RIDESAFE VR - GAME DESIGN DOCUMENT                                                                   BETA FINAL v0.9

Module variation controls

Modul  Fixed validation             Variable controls                         Rider start -> action
e

M1     Equipment/inspection         Object arrangement, vehicle profile       Stationary exit -> select -> inspect -> go/no-go

       completeness

M2     Controlled speed, scan and Geometry, sign, signal, traffic and         Upstream -> scan -> slow/position ->
                                                                              yield/proceed
       priority                     pedestrian timing

M3     Look, create space, act      Hazard, side, occluder, rain/surface and  Before occlusion -> reduce speed/create
                                    timing                                    margin -> act
       smoothly

M4     Avoid blind/swept path; safe Vehicle, curb, posted speed, oncoming gap Behind vehicle -> wait/scan -> wait or clear

       wait/pass                                                              pass

M5     Attention stop-first;        Notification/fatigue, sign,               Greenway -> manage attention ->
                                    pedestrian/oncoming user                  slow/wait/communicate
       vulnerable-user priority

Technical baseline

Quest 3 standalone; Unity LTS; OpenXR; XR Interaction Toolkit plus Meta XR SDK only where required. Target 90 FPS, 72
FPS hard minimum. Offline-first deterministic state machines, reusable actor/path systems, decision-gate state restoration,
local scoring, audio mixer, subtitles/localization and admin lifecycle. Use baked lighting, occlusion culling, LODs, GPU
instancing, texture atlases and object pooling. No runtime generative AI.

Production workstreams

 - Rig and controller mounts
 - Calibration and tracking
 - Controls and comfort
 - NPC/scenario systems
 - Feedback asset library
 - Assessment logic
 - UI/audio/accessibility
 - Local privacy/session lifecycle
 - QA and performance

Board and production reference - Figma Version 11                                                                      Page 21
RIDESAFE VR - GAME DESIGN DOCUMENT                 BETA FINAL v0.9

Production Roadmap and Governance

Phas Duration  Name                                Deliverable
e              Production lock
               Hardware/control prototype          Close critical rules, vehicle/control assumptions, privacy, timing, route, pass
0  2-3 weeks   Module 2 vertical slice             contract and asset list
               Core hazard expansion
1  4-6 weeks   Complete content                    Mounts, steering, brake/throttle mapping, tracking loss, calibration, comfort and
               Integration and certificate         performance benchmark
2  6-8 weeks   flow
               Release readiness                   Complete first attempt, repair, transfer, accessibility, audio, scoring, admin
3  8-10 weeks                                      reset and Quest QA

4  6-8 weeks                                       Build Modules 3 and 4, large vehicles, pedestrians, occlusion, overlays and
                                                   variation controls
5  6-8 weeks
                                                   Build Modules 1 and 5, onboarding, route connectors, shared path and
6  4-6 weeks                                       attention/fatigue representation

                                                   Full session, five transfers, binary result, external handoff, localization and
                                                   device matrix

                                                   Regression, representative-user review, SME approval, performance, SOP and
                                                   release candidate

Planning range and peak team

Sequential estimate: 7-10 months, with overlap after the vertical slice. Peak staffing: 6-9 FTE - product owner/producer,
learning designer, fractional road-safety SME, two Unity/XR engineers, one to two 3D/technical artists, UX/UI, fractional
audio/localization and QA ownership plus test support.

Definition of done

 - Five objectives, critical rules and changed transfers implemented.
 - One continuous first attempt and one continuous transfer per module.
 - Error-feedback matrix complete; no endless remediation.
 - Deterministic pass criteria reliable and reviewed by SME.
 - Controls/calibration stable; 72 FPS minimum maintained.
 - Comfort/accessibility review passed.
 - No production identity or persistent telemetry.
 - Facilitator and admin handoff documented.

Board and production reference - Figma Version 11  Page 22
RIDESAFE VR - GAME DESIGN DOCUMENT                                                    BETA FINAL v0.9

Governance and change control

Owner                     Decision authority                                          Required review
Product owner / producer  Scope, schedule, version and acceptance                     All production-lock changes
Learning designer +       Objectives, critical rules, feedback and transfer validity  Every rule, scenario and threshold change
road-safety SME
XR technical lead         Controls, rig, architecture and performance                 Hardware/control profile and state
                                                                                      restoration
Art / technical-art lead  Asset budget, route kit, readability and optimization       World or asset-count expansion
Assessment + QA owner     Event reliability, pass contract and regression             Transfer and near-miss threshold changes
Privacy/legal owner       Production data boundary and external form                  Exported field, retention or access changes

Version rule: Any change to a critical rule, changed-transfer scenario, control profile, speed threshold or pass threshold
increments the content version and triggers scenario regression, SME review and updated facilitator release notes.

Board and production reference - Figma Version 11                                     Page 23
RIDESAFE VR - GAME DESIGN DOCUMENT                 BETA FINAL v0.9

Open Decisions and Production Backlog

Decision blockers

 - Jurisdiction/local-rule wording and e-moped launch inclusion.
 - Final front/rear brake mapping per rig.
 - 3D mount geometry, handlebar range, seated/standing dimensions and tracking reliability.
 - Final X/Y/left-grip bindings after prototype.
 - Vehicle speed, acceleration, braking and steering-gain thresholds.
 - Localized mph/km/h values, posted/advisory speed signs and condition-safe grading thresholds.
 - Per-module pass and near-miss thresholds.
 - Headset-only versus companion admin UI.
 - Fatigue-cue representation without a detection claim.
 - Seated scooter accommodation.
 - Languages, recorded narration versus TTS.
 - External form ownership, notice, access, retention and certificate expiry.
 - Final art fidelity, route length and asset counts after graybox.
 - Final base/maximum time after usability testing.

Backlog actions

 - Bench-test mount, calibration and tracking-loss behavior.
 - Graybox all decision gates and verify 5-8 second observation windows.
 - Author the complete error-to-feedback matrix.
 - Approve transfer acceptance criteria per module.
 - Prototype blind-zone, stopping-envelope, wet-surface and gap overlays.
 - Validate stopping envelopes and speed thresholds for every vehicle profile and locale unit.
 - Complete representative-user accessibility review.
 - Run audio/subtitle intelligibility tests.
 - Build and test the Quest performance benchmark scene.
 - Complete legal and SME rule review.
 - Dry-run the admin handoff and external form.
 - Complete the regression/version checklist.

   Scope discipline: This beta-final document is limited to the current five-module learning architecture, delivery scope,
   technical baseline, governance and decision backlog required to estimate and build the experience.

Board and production reference - Figma Version 11  Page 24

```
