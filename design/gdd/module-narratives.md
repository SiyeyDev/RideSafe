# RideSafe VR â€” Module Narratives v2.0

> Text extracted with pdftotext from `Docs/design/RideSafe_UX_UI_v3.zip` â†’ `uploads/RideSafe_VR_Module_Narratives_v2.0.pdf` on 2026-10-16. Source of truth is the PDF. Local only (gitignored).

```text
                                                     RIDESAFE VR

               RideSafe VR

                     Module Narrative and Experience Blueprint

                                                            Version: 2.0
                              Status: Production narrative companion to the canonical GDD
                          Canonical specification: RideSafe_VR_GDD_v2.0_Evidence_Based.md
Purpose: Give design, engineering, art, audio, analytics, QA, localization, research, and instructional teams one
  shared picture of what the learner experiences, in what order, why each event exists, and what the system

                                                               records.

                                                    Production reference - English master
RIDESAFE VR - NARRATIVE

Document map

Use Word's Navigation pane or the PDF bookmarks to move through the document. Major sections are
listed below.
01 1. How to use this document
02 2. The session's continuous story
03 3. Shared interaction grammar
04 4. Module 1 narrative - Safety elements and vehicle readiness
05 5. Module 2 narrative - See and be seen at conflicts
06 6. Module 3 narrative - Manage speed, surfaces, and escape space
07 7. Module 4 narrative - Stay out of heavy-vehicle and curbside traps
08 8. Module 5 narrative - Choose whether and how to ride
09 9. End-to-end adaptive flow
10 10. Global production state model
11 11. Shared data and event expectations
12 12. Narrative acceptance checklist
13 13. Decisions that remain intentionally open
14 Appendix A - Module element inventories

                                                                                                                          Page 2
RIDESAFE VR - NARRATIVE

1. How to use this document

The canonical GDD defines what must be trained and measured. This companion describes how the
experience unfolds. It is intentionally written as a chronological story, but it is not a screenplay that
forces every learner through the same outcome. RideSafe is an adaptive training system. The world
reacts to the learner's choices, so a safe first attempt and an unsafe first attempt create different
moments while preserving the same instructional construct.

Three rules apply throughout the narrative:

   1. A described consequence is conditional. It occurs only when the learner's action earns it. The
       system never manufactures a mistake after correct performance.

   2. A described number is not automatically a final threshold. Distances, speeds, timings, and
       tolerances that affect scoring remain calibration parameters until the hardware and pilot establish
       valid device-specific values.

   3. A described road action is not a universal legal rule. The VR scene trains visibility, control,
       prediction, space, and decision mechanisms. Exact duties - such as sidewalk access, lane position,
       dismount requirements, maximum speed, equipment, and passing rules - come from the selected
       jurisdictional web rule pack.

The rule pack's jurisdiction stack, content fields, source hierarchy, publication lifecycle, Florida/Bogotá
coverage policy, and module handoffs are defined in RideSafe_Web_Rule_Pack_Content_Spec_v1.0.md.
This narrative refers to those records but does not duplicate or hard-code their legal values.

The narrative uses "the learner" rather than a fixed character. The learner may be new or experienced,
may use a standing e-scooter or an e-bike, may complete the experience seated or standing where
supported, and may encounter a Florida/U.S. or Bogotá/Colombia visual pack. The underlying skill
definition and telemetry remain stable across those variants.

2. The session's continuous story

The experience begins in a calm, controlled mobility hub rather than in traffic. The visual language
establishes that RideSafe is an assessment and practice environment, not an arcade. A short comfort
and control setup confirms language, device, handedness, seated or standing mode, reach, captions,
audio alternatives, and motion-comfort settings. The learner sees a plain explanation: they will make
decisions without help, receive feedback when useful, practice, and then face a changed situation. The
interface does not promise that passing proves future crash avoidance.

The experience forms one continuous ride-day story:

                                                                                                                          Page 3
RIDESAFE VR - NARRATIVE

   · Pre-modules: The learner holds both Meta Quest controllers normally and selects language,
       location, and vehicle through direct pointing and confirmation.

   · Module 1: With the controllers still handheld, the learner directly selects personal safety elements
       and identifies the vehicle components that belong in a pre-operational diagnostic. Two separate
       adaptive video reviews then demonstrate the correct elements and the complete device-specific
       checklist.

   · Mounting transition and control-familiarization gate: After Module 1, the experience enters
       mixed reality/passthrough, instructs the learner to request facilitator help, and guides the
       facilitator in securing both controllers in the physical handlebar supports. A mandatory, non-
       scored check then verifies the mounts, tracking, rider position, neutral steering, inputs, launch,
       turning, both-brake stopping, pause, and recenter before traffic.

   · Module 2: The learner leaves the protected area and enters a low-speed street network where
       sight lines and actor intentions matter.

   · Module 3: The route becomes physically demanding: surfaces, grade, parked vehicles, stopping
       distance, and escape space compete for attention.

   · Module 4: The learner reaches a busier curbside corridor where large vehicles create blind-zone
       and swept-path traps.

   · Module 5: The story shifts to self-regulation. A later ride opportunity introduces social pressure,
       impairment or severe fatigue, phone and load management, darkness, glare, and shared space.
       The learner must first decide whether to ride at all.

Transitions preserve continuity without implying that every module occurs on the same literal street or
day. A short neutral interstitial can summarize the previous skill, load the next environment, and allow a
comfort check. Baseline and changed-transfer scenes never reveal their scoring targets through arrows,
points, target bands, colored "safe zones," or a continuous speed HUD.

2.1 Pre-module configuration flow

The learner begins with both Meta Quest controllers handheld and tracked. This is Phase 1 of the
canonical interaction architecture. Pre-module configuration and Module 1 deliberately use direct
pointing, selection, and object/component interaction because the Spanish production brief and
meeting record describe Module 1 as a hands-on selection and diagnostic experience. The learner does
not mount the controllers before Module 1 and does not remove them from the supports after the
mounting transition.

The pre-module interface presents three decisions in order:

   1. Language: Select English or Spanish, with room for additional languages later.

                                                                                                                          Page 4
RIDESAFE VR - NARRATIVE

   2. Location/context: Select the deployment context that loads the Florida/U.S. or Bogotá/Colombia
       environment and the appropriate jurisdictional rule pack. More local city/county choices may be
       resolved by the web profile or facilitator configuration rather than by an excessively long VR
       menu.

   3. Vehicle: Select standing e-scooter or e-bike. The selected vehicle persists throughout the session.
       Module 1 videos, component checklist, control mappings, physics, telemetry thresholds, and later
       scenes load from this choice.

Each choice uses large world-space cards positioned in a comfortable forward field of view. The learner
points with either handheld controller and confirms with its trigger or designated face button. A
back/cancel input remains consistent across all three screens. The active card changes border, scale, and
label - not color alone - and the learner must explicitly confirm the final vehicle because it changes the
rest of the experience. Head-gaze with dwell, sequential scanning, or a single-switch path may be
enabled as accessibility alternatives, but dwell is not the default because accidental selection would be
likely.

The pre-module flow also performs comfort and control setup: seated/standing mode, handedness or
button remapping where supported, captions, audio alternatives, motion comfort, and a simple
confirm/back input check. It does not teach riding yet.

2.2 Phase 1 interaction grammar - handheld configuration and Module 1

All pre-module and Module 1 selections use one direct-interaction grammar:

   · Point/focus: Either handheld controller projects a short, stable ray or uses near-touch when an
       object is within comfortable reach.

   · Inspect: Pointing at or selecting an item reveals its name and a neutral close view without
       revealing whether it is correct.

   · Select/deselect: The learner uses the trigger to take, place, tag, or toggle the focused item. The
       selected state uses icon, text, and shape treatment in addition to color.

   · Direct object action: When meaningful, the learner virtually takes a helmet or protective element,
       places it in the preparation area, or points to a vehicle component. The interaction measures
       selection and recognition; it does not claim real helmet fitting, real tactile inspection, or real
       maintenance skill.

   · Review current choices: A reachable world-space control opens a neutral tray or checklist
       showing what the learner chose, not what the system considers correct.

   · Finish section: The learner points to and confirms "Review my choices." A second confirmation
       prevents accidental submission.

                                                                                                                          Page 5
RIDESAFE VR - NARRATIVE

   · Back/cancel: A consistent controller input returns to the previous item or screen without clearing
       prior choices.

   · Accessibility: Sequential scanning, dwell selection, subtitles, audio descriptions, and remapped
       confirm/back inputs are available when configured.

All Phase 1 targets must fit the calibrated reach envelope and work from a stationary position. The
learner may use either controller, so no critical task depends on one hand. Physical handling remains
limited to the controllers; the system must not suggest that a virtual action proves the fit, resistance,
pressure, or integrity of a real object.

2.3 Phase 2 interaction grammar - mounted riding

Phase 2 begins only after Module 1. Mixed reality/passthrough shows the real handlebar and controller
supports while a short animation identifies the left and right mounting positions. The learner is
instructed to stop interacting, keep the handlebar neutral, and ask the facilitator for help. The facilitator
physically seats and secures each controller, confirms straps or cables cannot interfere, checks tracking
and reach, and places the learner in the selected seated or standing riding position. The learner then
uses a mounted input to acknowledge readiness; the final production mapping may use one dedicated
accessible button or a deliberate both-brake hold.
From the first control-familiarization movement through Modules 2-5, both controllers remain
mounted. Riding input uses handlebar rotation plus the approved throttle or assist and brake controls.
Head orientation provides scanning and attention-proxy data. Any additional response must use a
reachable mounted control, a stop-safe world-space interface, or a facilitator-observed protocol; the
design must never ask the learner to pull a controller out of its support while riding.

3. Shared interaction grammar

Before the module narratives, every team should understand the repeated grammar.

3.1 Unguided baseline

The learner receives only the goal and necessary control instructions. The environment provides natural
cues - sight lines, movement, sound, surface appearance, signals, body orientation - but no instructional
overlay. Telemetry begins before the first critical cue so the system can distinguish anticipation from
reaction.

                                                                                                                          Page 6
RIDESAFE VR - NARRATIVE

3.2 Earned consequence

If the learner makes an unsafe choice, the consequence reveals what the decision removed: time,
visibility, stability, or space. Consequences remain survivable and reversible. They may include a horn, a
vehicle braking, a pedestrian stopping, a door beginning to open, a narrowing gap, surface vibration,
wheel deflection, a wobble, or an object becoming unstable. The system may slow or freeze before
impact. It avoids gore, impact spectacle, blame, ridicule, and score punishment designed to shame.

3.3 Explanatory debrief: pinpoint the error and show the correct action

The world pauses in a calm review state. Feedback does both jobs, in this order:

   1. Pinpoint the learner's observed decision. The system identifies the earliest material omission or
       unsafe action - what the learner selected, ignored, looked at, failed to verify, or did too late. It
       does not merely display a generic "incorrect" message.

   2. Demonstrate the correct action. A short explanation or counterfactual shows the complete
       correct behavior and why it preserves time, visibility, stability, or space.

For moving scenarios, a trajectory or timeline may show when the cue became available, where the
learner looked, when speed changed, when braking began, and how the margin developed. The
comparison must clearly label "your attempt" and "safer response" without humiliating the learner. For
Module 1 selections, the debrief uses a device-specific video and checklist comparison rather than a
trajectory replay.

If the learner performed correctly, the debrief confirms the relevant action and may still show the
complete reference behavior. It never invents an error. Legal notes are labeled as jurisdictional and link
to the web layer rather than being presented as universal facts.

3.4 Coached repair: make the correction understandable

Coached repair is the first active repetition after feedback. It intentionally makes the relevant cue easier
to perceive or the required action easier to organize. Temporary support may highlight one sight line,
ask for a prediction, show a stopping or clearance band, reveal a swept path, or give one concise cue
such as "look beyond the obstruction."

This step is supposed to be easier than independent assessment. Its purpose is not to prove mastery; it
lets the learner correctly execute a behavior they just misunderstood. To prevent the repair from
becoming trivial, coaching is targeted only to the failed skill, appears at the decision point rather than
throughout the whole scene, and does not give a complete step-by-step solution when a smaller cue is
sufficient.

                                                                                                                          Page 7
RIDESAFE VR - NARRATIVE

3.5 Faded-support confirmation: prove the cue is no longer needed

Faded support is a distinct repetition, not the same action as coached repair. The scene remains largely
familiar, but the strongest prompt is removed. If a second level is needed, only a naturalistic reminder
remains; otherwise the learner repeats with no prompt.

The purpose is diagnostic: coached repair shows that the learner can perform with help, while faded
confirmation tests whether the learner can reproduce the correction after help is withdrawn. Prompted
success is stored as coached performance. Only the faded attempt can qualify as independent
performance in the trained scene.

3.6 Changed transfer: test generalization

The transfer attempt changes at least two material variables when feasible. A new actor, geometry,
timing, light level, surface, device response, or occluder prevents memorization. The transfer scene is
unprompted, and the system gives no corrective feedback during the attempt. Its result is stored
separately from baseline, coaching, and faded confirmation.

Feedback appears immediately after the transfer attempt. The review identifies which critical skill or
supporting variable met criterion, which did not, and whether the failure reflects perception, prediction,
control, space, decision, or a jurisdictional rule. The learner sees only the few variables needed to
understand the outcome, while the complete telemetry remains available to the facilitator or analytics
system.

3.7 What happens when transfer is not passed

Transfer is not judged by simply counting failed variables. One critical failure can matter more than
several small deviations. Each active-skill module therefore defines critical behaviors and supporting
measures.

   · If all critical behaviors pass and the module composite meets criterion, the learner completes the
       module. A single supporting miss may be shown as a coaching point or future booster without
       forcing another full attempt.

   · If one critical behavior fails, or one skill domain falls below its minimum, the system gives a
       concise post-transfer explanation, returns to targeted coached repair, removes the cue in a faded
       confirmation, and then launches one new unprompted transfer variant.

   · If two or more critical behaviors or skill domains fail, the system remediates each failed domain in
       short targeted segments before the second transfer. It does not replay the entire module unless
       the failures are distributed across the complete route.

                                                                                                                          Page 8
RIDESAFE VR - NARRATIVE

   · A maximum of two unprompted transfer attempts per module in the same session is
       recommended: the initial transfer and one post-remediation transfer. Repeating more
       immediately risks fatigue and memorization.

   · If the learner fails a critical behavior on the second transfer, the result is not yet demonstrated.
       Certification or module mastery is withheld, but the learner may continue the remaining
       experience in practice/observation mode when safe. A new transfer assessment occurs in a later
       session or scheduled booster using a fresh variant.

   · If the second transfer contains only a supporting miss and all critical behaviors pass, the module
       may complete with a targeted improvement note, subject to the prespecified composite
       threshold.

The exact composite, critical thresholds, and whether a failed critical gate allows immediate progression
must be approved before pilot testing. The two-attempt policy should be treated as the production
recommendation unless the evaluation protocol establishes a different limit.

4. Module 1 narrative - Safety elements and vehicle readiness

4.1 Narrative purpose and scope decision

Module 1 answers two questions before the learner moves: What should I prepare or avoid before
riding, and what parts of this specific vehicle should I diagnose before deciding it is road-ready?
The module is intentionally limited to selection, recognition, diagnosis, interpretation, and explanatory
feedback. It does not contain the protected riding course, braking assessment, turn, shoulder check, or
changed riding transfer previously described in this section. Removing those riding tasks keeps Module 1
coherent and leaves room for two distinct instructional reviews: personal safety elements and pre-
operational vehicle diagnosis.
Vehicle familiarization is not eliminated. It moves to a short mandatory control-familiarization gate
immediately after Module 1 and before the first traffic exposure in Module 2. This protects the validity
of Module 2 by preventing unfamiliarity with the fixed handlebar from being mistaken for poor hazard
judgment.
Provisional learner time: 3-4 minutes, subject to prototyping the two adaptive video reviews. If the
product must meet a stricter total-session limit, shorten or combine review clips; do not remove the
pre-Module 2 familiarization gate.

                                                                                                                          Page 9
RIDESAFE VR - NARRATIVE

4.2 Entry conditions inherited from pre-modules

Language, location, accessibility settings, and vehicle have already been selected with the controllers
handheld. The learner retains both controllers for direct Module 1 interaction. The choice of standing e-
scooter or e-bike determines:

   · The vehicle model displayed in the garage.
   · The components available for diagnosis.
   · The diagnostic checklist and reference video.
   · Later control mappings, physics, telemetry thresholds, and scenarios.
   · The applicable legal follow-up in the web layer.
The system must not silently switch devices during or after Module 1. If the learner changes the vehicle
selection, the device-specific Module 1 state resets and the correct checklist/video package loads.

4.3 Garage context and movement

The learner enters a calm garage or mobility-preparation environment divided into two clear sections:
   1. Personal safety elements. A display wall, shelving system, or world-space catalog presents the
       items under consideration.
   2. Vehicle diagnostic. The selected e-bike or e-scooter is presented with its relevant components
       and a neutral checklist panel.

The learner sees the garage from first-person view but does not need to physically walk, teleport, or
learn a locomotion mechanic. After completing the first section, the system uses a short comfort-safe
guided transition - such as a fade, controlled viewpoint shift, or environmental reveal - to place the
second section in front of the learner. Automatic movement must be tested for motion comfort; a fade
transition is the fallback if camera movement causes discomfort.
The Florida/U.S. and Bogotá/Colombia versions use different reference environments, vehicles, signage,
architecture, and context photography while preserving the same interaction geometry and decision
categories.

4.4 Opening orientation

The module begins with a short explanation delivered simultaneously through voice-over, readable text,
and simple visual examples. It tells the learner what they will do without revealing the answers:

   · First, choose the personal safety elements they believe are necessary or useful and leave aside
       elements they believe should not be used.

                                                                                                                         Page 10
RIDESAFE VR - NARRATIVE

   · Second, decide which components of their selected vehicle should be included in a pre-
       operational diagnostic.

   · After each decision section, compare their choices with a complete expert demonstration.
The orientation explains direct pointing, inspecting, taking or tagging an item, selecting or deselecting,
reviewing current choices, submitting, and going back. A brief rehearsal uses neutral sample objects
unrelated to the scored items. The learner proceeds only after demonstrating a point-and-select action
with at least one handheld controller.

4.5 Section A baseline - Select personal safety elements

The learner faces a curated set of objects arranged within a comfortable stationary reach envelope.
They point to, virtually take, and place chosen elements into a clearly marked "my ride preparation"
area, or use an equivalent direct selection when a wearable animation would be misleading. A helmet
can be lifted and placed on a virtual preparation stand or avatar target; the system must not imply that
this proves real-world size or strap fit. A second action returns or deselects an item. Selected items
retain a persistent marker so the complete set remains reviewable.
The item set uses content categories defined before production:

   · Core safety elements: Items the curriculum expects the learner to select for the presented ride
       and conditions.

   · Condition-dependent or recommended elements: Items that are useful in the presented context
       but must not be mislabeled as universally legally required.

   · Optional neutral elements: Plausible items that are not necessary for the safety objective and are
       not harmful.

   · Inappropriate or risk-increasing elements: Items or configurations that clearly compromise fit,
       visibility, attention, or vehicle control in the presented context.

The proposed launch list below must be approved through the evidence and legal-content process and
then adapted to the selected reference vehicles. A dark garment, headphone, phone, or accessory must
not be called universally "harmful" merely because it is less ideal; the content team must define the
actual mechanism and jurisdictional dependency. Legal requirements appear later in the web layer.
4.5.1 Recommended launch selection inventory
The first production build should use the following compact eight-choice board. Each visual must make
its condition legible without revealing its scoring category.

                                                                                                                         Page 11
RIDESAFE VR - NARRATIVE

Code  Choice shown to the learner        Expected interpretation              Scoring treatment                 Evidence
P1
P2    Undamaged, activity-               Select                               Universal critical preparation    E05, E10, E21-E24
P3    appropriate helmet in the                                               element; explain injury
P4    correct size, shown level and                                           mitigation and residual risk
P5    fastened
P6
P7    Cracked, impact-damaged,           Do not select as ready-to-use        Inappropriate/risk-increasing;    E21-E24
      visibly wrong-size, tilted, or                                          explicitly contrast condition,
P8    unfastened helmet                                                       position, fit, and fastening

      Secure footwear and contained      Select the safe configuration        Critical only when the            E21, E23
      clothing: tied laces and no                                             alternative visibly creates
      loose fabric near moving parts                                          entanglement or control
      or controls                                                             obstruction

      Untied laces, loose pant           Do not select                        Inappropriate/risk-increasing;    E21, E23
      leg/scarf, or rainwear that can                                         explain the actual
      contact the drivetrain or                                               entanglement or control
      obstruct control                                                        mechanism

      Bright or retroreflective outer    Select when the presented            Condition-critical; never         E21, E23
      layer                              condition is darkness, low           present as a universal clothing
                                         visibility, or rain; acceptable but  law
                                         not required in clear daylight

      Knee/elbow pads, gloves, or        Optional                             Optional-neutral; selection and   E22; no broad
      eyewear                                                                 omission receive no penalty       every-ride
                                                                                                                U.S./Colombian
                                                                                                                requirement
                                                                                                                located

      Backpack, rack, or basket          Select when a load is presented      Universal control principle;      E21, E22, E26-E28
      configuration that secures the                                          exact device load limit remains
      presented object without                                                web/operator content
      blocking vision or controls

      Object held in one hand,           Do not select                        Inappropriate/risk-increasing;    E07, E21, E22, E26-
      hanging from the handlebar, or                                          the moving decision and its       E28
      positioned so it blocks vision or                                       modeled attention/control
      a control                                                               consequence are revisited in
                                                                              Module 5, but physical hand
                                                                              release is not directly measured
                                                                              while controllers are fixed

The base board does not include phone use, headphones, passenger behavior, or detailed night
equipment. Those are meaningful behaviors, not merely equipment-recognition questions, and receive
active treatment in Module 5. A later changed-example retrieval may substitute one of them, but
Module 1 must not consume the practice time reserved for the moving lesson.

The learner is free to inspect items in any order. The system records focus, inspection time, selection,
deselection, final submission, and category-level errors. No correct-answer counter, green check, red X,
or visible complete checklist appears before submission. The learner can open a neutral tray showing
only what they have selected.

                                                                                                                          Page 12
RIDESAFE VR - NARRATIVE

When satisfied, the learner chooses "Review my choices" and confirms. This locks the baseline selection
and launches the first feedback section.

4.6 Section A feedback video - Correct elements and correct use

The first feedback section performs two jobs: it pinpoints the learner's choices and teaches the
complete reference behavior.
The system begins with a compact comparison board:

   · Correctly selected core items are acknowledged.
   · Core items that were omitted are identified.
   · Optional or condition-dependent items are labeled accurately rather than treated as errors.
   · Selected inappropriate/risk-increasing items are identified with the specific risk mechanism.
The comparison then becomes an adaptive video sequence. It is one coherent review section assembled
from short modular clips, not an uncontrolled series of long videos. The sequence includes:
   1. A brief overview of the complete appropriate set for the presented scenario.
   2. A clip for each selected core element showing its purpose and correct use when use must be

       demonstrated.
   3. A corrective clip for each omitted core element explaining why it matters and how it is used.
   4. A risk clip for each selected inappropriate element showing the relevant attention, fit, visibility,

       entanglement, or control problem.
   5. A concise final frame showing the complete correct set and distinguishing core, condition-

       dependent, optional, and inappropriate categories.
The learner's choices remain visible beside the reference set so the video does not become generic
instruction detached from performance. Captions and audio alternatives are required. The learner may
pause, replay the current clip, or continue using handheld pointer controls.
For an entirely correct selection, the sequence still demonstrates correct use, but it omits unnecessary
corrective clips. For several errors, the system prioritizes core omissions and risk-increasing selections so
the review remains short enough to retain attention.

4.7 Transition to the vehicle diagnostic

After the safety-element review, the system states that personal preparation is only one part of
readiness; the vehicle must also be checked. The environment transitions to the selected vehicle. The
learner does not operate it yet.

                                                                                                                         Page 13
RIDESAFE VR - NARRATIVE

The vehicle is oriented so the relevant components can be viewed comfortably. A rotate-view input or
controlled automatic viewpoint sequence may expose the front, side, cockpit, wheels, and rear without
asking the learner to walk. Camera motion must remain comfort-safe. The e-bike and e-scooter use
separate component maps, checklists, close-up images, and videos.

4.8 Section B baseline - Choose components for pre-operational diagnosis

The learner sees the selected vehicle and a neutral checklist panel. They decide which components
should be diagnosed before riding. The visible checklist grows only with components the learner selects;
it does not reveal omitted answers before submission.

With the controllers still handheld, the learner points directly at components on the selected vehicle or
at a comfort-safe close view. Pulling the trigger tags or untags the component and adds or removes it
from "my pre-ride check." Where the production model supports it, a short virtual action may
demonstrate squeezing a brake lever, checking throttle return, or indicating tire and wheel condition;
this remains a recognition simulation without real resistance, pressure, or mechanical validation.

The component catalog is device-specific. It may include brakes, steering/handlebar security, wheels
and tires, lights or reflectors for the presented condition, battery/status or controls, frame/folding/latch
points where applicable, and other components approved for that device. Decoy or non-preoperational
components may appear, but the task must remain short and credible.

4.8.1 Universal diagnostic domains and criticality

Code  Domain               Learner-facing question                 Critical no-start examples                 Evidence
V1    Brakes
V2                         "Will the braking system engage         No resistance; absent, sticking, delayed,  E05, E07,
V3    Tires and wheels     normally and stop this device?"         or unpredictable engagement; visible       E21-E28
V4                                                                 cable/control damage
V5    Steering and load-   "Are the tires usable and the wheels
      bearing structure    secure and stable?"                     Flat or severely damaged tire;             E07, E21-
      Lights, reflectors,                                          loose/detaching wheel; strong lateral      E28
      and audible warning  "Are the handlebars, stem/neck, frame,  play or wobble
                           fork, deck, and joints secure and
      Electric status and  undamaged?"                             Crack, deformation, severe looseness,      E07, E21-
      controls                                                     detachment risk, binding steering,         E28
                           "Are the fitted visibility and warning  unlocked safety joint
                           devices present and working for this
                           condition?"                             Required light unavailable in              E21-E23,
                                                                   darkness/low visibility; otherwise record  E26-E28
                           "Is the battery/control system intact,  as supporting or jurisdictional
                           predictable, and free of a critical
                           warning?"                               Smoke, sparks, unusual heat/odor,          E07, E22,
                                                                   swelling, damaged or tampered battery,     E26-E28
                                                                   critical error, stuck throttle,
                                                                   uncommanded drive

                                                                                                              Page 14
RIDESAFE VR - NARRATIVE

Code   Domain            Learner-facing question                         Critical no-start examples                    Evidence

V6     Fault response    "If a critical condition is present, what       Learner chooses to ride despite a V1-V3       E05, E07,
                         should happen next?"                            or severe V5 fault, or despite a V4 fault     E22, E26-
                                                                         that is critical in the presented visibility  E28
                                                                         condition

For a shared vehicle, the reference response is do not unlock/start, report the problem through the
applicable service, and choose another vehicle. For a privately owned vehicle, the response is do not
ride until the condition is corrected by an appropriately qualified person. Module 1 teaches detection
and disposition, not user repair.

4.8.2 Standing e-scooter checklist package

Order  Component group                       Demonstrated check                           Classification
1      Overall condition
2                                            Scan for collision damage, modification,     Device-critical when severe
3      Stem, neck, handlebars, steering,     exposed cable, loose part, thermal
4      and folding/locking joint             warning, or critical display error
5      Deck/baseboard and visible load-
       bearing fasteners                     Show secure attachment, smooth steering,     Device-critical
6      Tires and wheels                      no abnormal fore-aft movement, and a
7                                            fully secured joint where fitted
8      Braking system
                                             Show intact, stable standing surface and     Device-critical; superficial cosmetic
       Throttle and electrical controls      no crack or severe looseness                 damage is not

       Lights, reflectors, brake light, and  Show usable tire condition/inflation as      Device-critical
       bell/horn where fitted                applicable, wheel retention, and absence
       Decision                              of severe play or wobble

                                             Identify every brake control and show        Device-critical
                                             normal resistance/engagement; complete
                                             rolling stop moves to familiarization

                                             Show neutral return, no sticking or          Device-critical
                                             unintended command, adequate status
                                             display, and no critical battery/controller
                                             warning

                                             Show function and secure attachment          Condition-critical or supporting; exact
                                                                                          requirement is local/device-specific

                                             Classify road-ready, needs                   Universal critical decision
                                             adjustment/service, or
                                             no-start/report/replace

Kickstand release, push-off, acceleration feel, actual braking distance, turning, and stopping are not
checklist selections. They are taught in the control-familiarization gate.

                                                                                                                       Page 15
RIDESAFE VR - NARRATIVE

4.8.3 E-bike checklist package

Order  Component group                        Demonstrated check                      Classification
1      Overall fit and structure
2                                             Show appropriate frame/seat             Insecure structure is device-critical; fine
3      Tires, wheels, and axles/retention     relationship, intact frame/fork, and    ergonomic adjustment is supporting
4                                             secure handlebar/stem
5      Both braking controls and visible                                              Device-critical
6      brake parts                            Show correct pressure range reference,
7                                             usable tread/sidewall, secure axle or   Device-critical
8      Chain, cranks, pedals, and gears       quick release, and no severe play
                                                                                      Broken, loose, or nonfunctional part is
       Seatpost, saddle, rack, and basket     Identify front/rear controls; show      device-critical; routine
       where fitted                           normal lever response and no obvious    cleaning/lubrication is supporting
                                              pad, rotor, cable, or hose defect       Device-critical when attachment is
       Battery, mount/lock, wiring, display,                                          insecure; load limits are web/device-
       assist/throttle controls               Show secure cranks/pedals, serviceable  specific
       Lights, reflectors, and bell/horn      chain condition/tension, and free
       where fitted                           drivetrain movement                     Device-critical for damage, thermal sign,
       Decision                                                                       critical warning, or uncommanded drive
                                              Show secure attachment and
                                              adjustment; confirm cargo does not      Condition-critical or supporting; exact
                                              obstruct vision or control              requirement is local/device-specific

                                              Show secure undamaged battery, no       Universal critical decision
                                              thermal warning/tampering, and no
                                              critical error or unintended drive
                                              command

                                              Show function and secure attachment

                                              Classify road-ready, needs
                                              adjustment/service, or
                                              no-start/report/replace

Pedal-assist engagement, throttle launch where fitted, actual brake response, turning, looking behind,
and dismounting are taught after Module 1 in the familiarization gate and later active modules.

The learner can review the checklist they built and then choose "Finish diagnostic selection." A
confirmation explains that the next section will compare their choices with the complete device-specific
inspection.

The system records components focused, selected, deselected, omitted, added unnecessarily, order,
time, and final submission. At this stage, it does not claim the learner physically performed the
inspection; it measures identification of components that should be diagnosed.

4.9 Section B feedback video - Complete checklist, method, and interpretation

The second feedback section is separate from the personal-elements review. It shows the complete pre-
operational diagnostic for the selected device and compares it with the learner's checklist.

                                                                                      Page 16
RIDESAFE VR - NARRATIVE

The sequence has five parts:

   1. Choice comparison: Display the learner's checklist beside the complete reference checklist.
       Correct selections, omissions, and unnecessary selections use text, icons, and shape treatment in
       addition to color.

   2. How to inspect: Demonstrate how each required component is checked on the selected e-bike or
       e-scooter.

   3. How to interpret: Show what an acceptable condition looks, sounds, or feels like and what
       observable condition makes the component questionable or unsafe.

   4. Decision: Demonstrate the appropriate outcome - road-ready, adjust before riding, remove from
       service, seek maintenance, or select another vehicle. The system must not suggest that every
       issue can be repaired by the learner.

   5. Complete recap: Present the final device-specific checklist in the intended practical order, while
       clarifying that recognition of every critical component matters more than memorizing an arbitrary
       order.

Production therefore requires at least two master video/checklist packages: one for the launch e-bike
and one for the launch standing e-scooter. The system may compose shorter clips adaptively according
to the learner's omissions and errors, but the complete checklist remains available for replay and web
follow-up.

If a selected component is condition-dependent rather than universally required, the video labels that
dependency. Legal equipment requirements and technical service instructions beyond the learner's role
remain in the web content or facilitator materials.

4.10 Module 1 feedback, completion, and scoring

Module 1 does not use coached physical riding, faded control support, or a changed riding transfer. Its
assessment unit is the two submitted selections followed by complete explanatory demonstrations.

The learner report separates:

   · Core safety elements correctly selected.
   · Core safety elements omitted.
   · Optional/condition-dependent elements selected or omitted without false penalty.
   · Inappropriate/risk-increasing elements selected.
   · Required diagnostic components selected and omitted.
   · Nonessential components added.

                                                                                                                         Page 17
RIDESAFE VR - NARRATIVE

   · Device-specific road-ready/no-start concepts understood through a short post-video confirmation
       if needed.

   · Video clips shown, paused, replayed, or skipped where skipping is allowed.
Because the correct answers are fully shown after each submission, immediately repeating the same
selection set would measure memory of the video more than transfer. Module 1 should therefore use
one of two approaches:

   · Treat the two baselines plus instructional reviews as completion training and assess retention later
       through a changed item set or web booster; or

   · Present a short changed confirmation using different examples or a different faulty/correct
       component image, not the identical selection board.

The recommended launch approach is the first: complete Module 1 after both reviews, flag critical
misunderstandings for later retrieval, and avoid extending the module with a redundant immediate test.

4.11 Mandatory control-familiarization gate before Module 2

The learner must not move directly from handheld Module 1 interaction into a scored traffic conflict.
The control-familiarization gate is outside Module 1, begins with the mounting transition, and occurs
immediately before Module 2.

The garage fades to a calm mixed-reality or passthrough setup state. A short animation shows the real
left and right controller supports and tells the learner: keep the handlebar centered, do not mount the
controllers alone, and ask the facilitator for help. The facilitator seats and locks both controllers, checks
their orientation and tracking, verifies that the learner can reach throttle or assist, front and rear brake
inputs, pause, and recenter, and confirms the selected standing or seated posture. If passthrough,
tracking, a mount, or a control is not valid, Module 2 remains locked.

When the physical check is complete, the learner confirms readiness through the approved mounted
input. The default prototype decision is a deliberate hold of both brake inputs because it simultaneously
proves reach and avoids accidental continuation; a dedicated accessible button remains an alternative
pending rig testing. The system then returns to the protected virtual practice space and verifies neutral
handlebar alignment before enabling propulsion.

The gate lasts approximately 60-90 seconds for a new learner and may be shorter for an experienced
learner. It occurs in a neutral protected space and is instructional, not an unguided safety assessment.
The system visibly teaches and lets the learner rehearse:

   · Activation or start input.
   · Acceleration or assist input.

                                                                                                                         Page 18
RIDESAFE VR - NARRATIVE

   · Steering response.
   · Progressive braking and a complete stop.
   · A gentle direction change or broad turn.
   · Looking left/right or behind while maintaining basic control, if required by later scenes.
   · Pause and emergency-stop input.
   · Recenter and safe resume.
Because the purpose is interface familiarization, cues may be obvious. The learner is not being certified
on this first exposure. The gate ends only after mount and tracking validity, neutral steering, activation,
controlled low-speed launch, broad turn, progressive use of both brakes, complete stop, and
stop/recenter are demonstrated. If one variable fails, the system repeats only that control with an
explicit cue. If several variables fail, it returns to the stationary mapping check before another low-speed
sequence. After two assisted gate sequences without stable start, turn, and stop control, Module 2
remains locked and the facilitator changes the setup, provides additional off-course practice, or ends the
riding session. This limit is a deployment safety rule, not a mastery score.

Control telemetry is stored as diagnostic context and used to interpret later performance, but it remains
separate from the Module 1 equipment/diagnostic score. A future validated version may promote
selected control measures into a formal readiness gate.

4.12 Closing transition

After the second Module 1 video, a clear transition explains that the learner has reviewed personal and
vehicle readiness and will now prepare the riding rig. The experience enters the mixed-reality mounting
flow described above; the facilitator, not the learner alone, secures the controllers.

After familiarization, the next message states that the learner can start, steer, and stop; Module 2 will
now test whether they can see and predict conflicts without instructional prompts. This moment is the
boundary between interface learning and safety-performance measurement.

The web layer receives Module 1 follow-up topics: helmet and age rules, equipment, passengers and
loads, vehicle class, wattage, assist/throttle definition, licensing, registration, title, insurance, and the
complete device-specific checklist.

4.13 Cross-team production picture

   · Design: Treat the experience as one two-phase architecture: handheld direct interaction through
       Module 1, then a single supervised mounting transition and mounted-only riding. Keep categories
       understandable without leaking answers. Separate the two submissions and two video reviews.

                                                                                                                         Page 19
RIDESAFE VR - NARRATIVE

· Engineering: Support handheld ray or near selection, confirm/deselect, back, neutral checklist
   state, adaptive clip assembly, device persistence, passthrough mounting guidance, mount/tracking
   validation, and a separate non-scored familiarization state.

· Art: Produce two garage/context packs, item cards or displays, device-specific component targets,
   e-bike and e-scooter close views, correct/faulty visual states, and readable checklist comparison
   screens.

· Video/content: Produce modular safety-element clips and two complete device packages showing
   the checklist, inspection method, interpretation, and road-ready/no-start decision. Keep clips
   concise and captioned.

· Audio: Provide voice-over and interaction confirmation without making sound the only diagnostic
   information. Record English and Spanish versions from one controlled script.

· Analytics: Store pre-module choices; item/component focus and selection; submission; category-
   level errors; clips displayed; review behavior; post-video confirmation; and familiarization
   telemetry in a separate namespace.

· QA: Verify handheld interactions and reach in Module 1; require facilitator mounting; prevent
   propulsion during passthrough; verify both controllers are correctly seated and tracked; verify
   every riding input is reachable while mounted; confirm no Module 2-5 interaction requires
   controller removal; and test both device packages, videos, checklists, and comfort transitions.

· Evidence/legal: Approve the item and component lists, define core/optional/condition-
   dependent/inappropriate categories, and keep legal mandates in the web rule pack.

· Facilitator: Before Module 1, confirm safe stationary handheld use. After Module 1, physically
   mount and secure both controllers, confirm handlebar stability, controller tracking, rider position,
   neutral alignment, emergency-stop access, and readiness to enter the control-familiarization gate.

4.14 Controller-dependent conflict register

The older design documents contain interactions that cannot all be measured literally once the
controllers are mounted. Production uses the following resolution:

Legacy interaction          Launch resolution with mounted controllers            Claim boundary / deferred option
Hand signaling
                            Teach the decision, timing, and purpose through a     Direct signal execution requires validated hand
Phone/context interaction   stopped explanation or facilitator-observed           tracking, a dedicated signal input, or facilitator
Hazard selection / freeze-  rehearsal; do not require a rider to remove a         observation
and-predict                 controller in motion
                                                                                  Measures stop-before-interaction choice, not real
                            Present the notification while moving; allow ignore,  phone handling
                            or require a complete safe stop before a mounted-
                            button world-space interaction                        Head pose is an attention proxy, not proof of eye
                                                                                  fixation or comprehension
                            In feedback or repair only, use head-directed focus
                            plus a reachable mounted confirmation input

                                                                                  Page 20
RIDESAFE VR - NARRATIVE

Legacy interaction        Launch resolution with mounted controllers          Claim boundary / deferred option
Route choice
Object/load manipulation  Offer branches through steering at a safe decision  Local legality and route availability remain in the
                          point or a stop-safe mounted interface              web rule pack
Shoulder checking
                          Complete direct object selection in handheld        Physical one-handed control or load securing is not
                          Module 1; later modules model the selected load     directly measured without added sensing
                          state and allow correction only after stopping
                                                                              Do not label head orientation as eye fixation;
                          Measure head orientation and lateral control while  calibrate acceptable movement by rig and comfort
                          both hands remain on the rig                        mode

These resolutions preserve the intended safety decisions while preventing the simulation from claiming
physical behaviors the launch hardware cannot validly observe.

5. Module 2 narrative - See and be seen at conflicts

5.1 Narrative purpose

Module 2 begins the street journey. Its central idea is that permission to move does not guarantee that
another road user has seen the rider or will yield. The learner must build a repeatable habit: expose the
sight line, scan, detect the earliest useful cue, predict the actor's likely path, adapt speed and position,
and enter only after the conflict is clear.

Target learner time: 3-3.5 minutes for three short baseline conflicts, one selective feedback/repair
sequence, and two changed transfer conflicts. If production requires all three conflict families to appear
again in transfer, the honest target becomes approximately 4-5 minutes rather than compressing cue
exposure or feedback.

The module is not a memorization test for one right-of-way rule. It is a perception and decision
experience whose legal answer is supplied by the active jurisdiction pack.

5.2 Environment and entry

The protected lane opens onto a compact low-speed district containing three anchor conflict families:
an occluded garage, driveway, or alley emergence; a locally governed intersection with an approaching
or turning vehicle; and a partially occluded pedestrian crossing. A stop, yield, or signal condition is the
legal wrapper around the intersection/turn conflict, not a fourth mandatory perceptual encounter.
Buildings, parked vehicles, vegetation, street furniture, transit elements, and road markings create
realistic occlusion without visual clutter that makes the cue impossible to detect.

                                                                              Page 21
RIDESAFE VR - NARRATIVE

The Florida/U.S. art pack may use a campus edge, mixed-use block, parking structure, and marked
crossing. The Bogotá pack may use tighter frontage, grade, mixed vehicle types, delivery activity, and
locally appropriate markings. The sequence of perceptual demands remains equivalent even when the
rule-pack answer or scene appearance changes.

Before motion begins, the system says only that the learner should travel to the destination and manage
conflicts safely. No scan diagram, target gaze, point counter, lane command, or continuous speed
display appears. Telemetry begins while the learner is still approaching the first occluder.

The learner never removes the Meta Quest controllers from the handlebar. When a freeze-and-predict
question is used in feedback or repair, the learner selects a direction or actor with head/gaze-directed
focus and confirms with an accessible mounted trigger or button. The interaction does not require
pointing with a free hand and is not used during unprompted baseline or transfer.

5.3 Beat 1 - Garage or driveway emergence

The first conflict is designed around incomplete sight lines. A wall, hedge, parked van, gate, or garage
edge blocks the learner's view of an emerging vehicle. At least one pilot-validated primary cue must
become readable: partial actor or wheel visibility, wheel rotation or angle, vehicle motion, lateral
movement, or gap closure. Indicator state, engine or gate sound, and driver body orientation may
corroborate the prediction. A shadow or reflection may add realism but must never be the only cue
required for a scored response.

The learner decides how quickly to approach and how to position for visibility while remaining within
the locally valid operating area. The system records cue availability, tracking validity, head/gaze entry
into the relevant conflict area, reacquisition after the scene changes, approach speed, distance from the
occluder, speed response, braking onset, commitment-line crossing, stopping margin, and closest
conflict margin. Head orientation is an attention proxy only; it cannot prove that the learner consciously
saw or understood the cue.

If the learner reduces speed, obtains the sight line, detects the cue, and waits or proceeds after
clearance, the vehicle interaction resolves normally. The system records independent success and
continues.

If the learner maintains speed or commits before the sight line is clear, the vehicle begins to emerge.
The consequence is calibrated to reveal the shrinking margin: the driver may brake, a horn may sound,
or the learner may need a sharp stop. The system freezes before impact if the available margin falls
below the comfort boundary.

                                                                                                                         Page 22
RIDESAFE VR - NARRATIVE

5.4 Debrief 1 - Occlusion and the earliest cue

The review returns to the point before the vehicle was fully visible. It pinpoints the learner's earliest
consequential error, shows the occluded region, and aligns the first usable primary cue with the
learner's path, speed, brake, and commitment. The explanation avoids hindsight language such as "you
should have known the car was there." Instead, it explains what could be known: the sight line was
incomplete, what cue became available, whether the learner acquired it, and whether the resulting
action preserved enough time.

If the learner acquired the conflict area but did not interpret it correctly, they are asked to predict the
vehicle's path from a frozen view. The response is recorded as prediction accuracy, not legal knowledge.
A short counterfactual then shows the correct behavior: a slower approach, a position that improves
mutual visibility where locally allowed, an earlier brake onset, and entry after clearance. Feedback
therefore both pinpoints the learner's error and demonstrates the safer action; it does not merely
announce that the result was wrong.

5.5 Coached repair 1

The scene resets with temporary assistance selected for the diagnosed break in the chain. If information
was not acquired, a subtle sight-line boundary or cue marker may show what remained hidden. If
information was acquired but the path was not predicted, the system uses one freeze-and-predict
prompt with expert explanation. If the actor was predicted but the learner did not slow, brake, or delay
commitment, a temporary stopping-margin or commitment reference appears.

Coach repair and faded support are two moments in one short instructional loop, not duplicate
assessment steps. The cue appears during the diagnosed action, then is weakened or removed as the
learner completes that repetition. By the end of the repair, only the natural cues remain. None of this
coached performance contributes to the transfer result.

5.6 Beat 2 - Locally governed intersection and turning conflict

The route leads to an intersection where an approaching or turning vehicle may cross the learner's path.
The learner must separate legal permission from conflict clearance. The selected rule pack determines
the stop, yield, signal, dismount, or position requirement. The portable VR construct remains stable:
approach under control, acquire the relevant conflict areas, observe actor movement, predict the likely
path, and enter only when clear.

A motor vehicle approaches or begins a crossing turn. Another object - such as a parked vehicle,
landscaping, or street furniture - creates a partial view on one side. Primary cues can include partial
vehicle visibility, wheel rotation or angle, speed or lateral-position change, and gap closure. Indicator

                                                                                                                         Page 23
RIDESAFE VR - NARRATIVE

state, body orientation, or spatial audio can corroborate the path but no single indicator guarantees it.
The system records each required conflict area, whether it was observable, tracking quality, acquisition
and reacquisition, prediction, stop position, accepted gap, entry speed, speed response, brake onset,
commitment, minimum time to collision, and any emergency response by the other actor.
"Scan complete" never means that the learner rotated their head through a prescribed arc. It means
that the relevant conflict information was available, the recorded attention proxy entered each required
area of interest with valid tracking, and the learner's prediction and action reflected the developing
conflict. If tracking is invalid, the attention field is not scorable; missing data is not converted into a
failure.
The scene should permit more than one safe timing choice. It must not reward an unnecessarily
aggressive gap merely because the learner remained technically collision-free. It also must not punish a
reasonable cautious pause as failure; excessive delay can be recorded separately for usability or
confidence calibration.
If the learner enters despite a closing conflict, the vehicle reacts naturally. A brake light, wheel angle,
body pitch, or horn reveals the interaction. The scene freezes before impact when needed. If the learner
behaves safely, the vehicle completes its movement or yields without manufactured drama.

5.7 Beat 3 - Pedestrian occlusion and crossing

The final baseline conflict introduces a pedestrian partially hidden by a vehicle, shelter, sign,
landscaping, or curb activity. The pedestrian's body orientation and movement create early cues before
full path entry. The learner must adapt speed, yield as required, and preserve comfortable passing
space.
The system records cue availability, conflict-area acquisition proxy, speed change, brake onset,
commitment, yield, stopping position, minimum clearance, and whether the pedestrian was forced to
brake, stop, retreat, or evade. It does not hard-code a universal sidewalk or crossing rule. The rule pack
determines access and duty; the portable VR score measures whether the learner notices and protects
the pedestrian conflict.
If the pedestrian has to stop abruptly because the learner enters, that response becomes the
consequence. The system does not depict an impact.

                                                                                                                         Page 24
RIDESAFE VR - NARRATIVE

5.8 Integrated debrief and error diagnosis

At the end of the baseline route, the system presents a short conflict timeline. It selects the earliest and
most consequential break in the perception-to-action chain across the three interactions. The visual
structure is consistent:

   1. What was visible and what was hidden.
   2. What the other actor was beginning to do.
   3. Where the learner looked.
   4. Whether speed preserved enough time.
   5. Where braking or commitment occurred.
   6. What safer alternative preserved visibility and space.
The system distinguishes information-acquisition errors, prediction errors, margin/action errors,
pedestrian-protection errors, and legal-rule errors. A learner who acquired and safely managed the
conflict but applied the wrong local rule receives web remediation and a local-rule retest; the universal
perception score remains intact. A learner who never acquired the actor receives a sight-line repair. A
learner who acquired the actor but entered anyway receives a margin/action repair. The legal
explanation links to the web rule pack and carries jurisdiction, effective date, and verification metadata.

5.9 Coached integrated repair

The learner repeats only the diagnosed conflict type, not the full baseline route. Supports are minimal
and targeted. Possible supports include a freeze-and-predict moment, a brief sight-line overlay, a cue
marker, or a stopping reference. The system fades each support within that repair and removes it before
transfer.

The learner should experience that scanning is not head movement for its own sake. Relevant
information must be acquired and must influence prediction and action when a conflict exists. Analytics
therefore store cue availability, attention proxy, prediction, speed/brake response, commitment, and
actor outcome as distinct events.

5.10 Changed transfer

The transfer route is recognizably new and contains two encounters: one vehicle conflict sampled from
the driveway or intersection/turn family and configured with an incomplete sight line, plus one
pedestrian conflict. This pairing can cover M2-C1-C4 without replaying all three baseline anchors. The
garage may become an alley or parking exit; the approaching car may become a turning van or
motorcycle; the pedestrian may approach from the opposite side; the signal phase, light, actor timing,

                                                                                                                         Page 25
RIDESAFE VR - NARRATIVE

occluder, and geometry may change. At least two material variables change in each encounter, cue
visibility is revalidated, and all instructional overlays are removed.

The learner receives only a destination. The system scores each conflict independently and calculates a
module-level transfer result without averaging baseline or coached performance into it. If the two
sampled encounters do not collectively cover all critical domains, the uncovered domain must be
demonstrated in a later transfer item rather than inferred from coaching.

5.11 Mastery gate, retry rule, and transition

Module 2 is a critical gate. Four critical behaviors are scored separately:

   · M2-C1 - Margin under occlusion: preserve a recoverable stopping or yielding margin while a
       conflict origin remains unresolved.

   · M2-C2 - Relevant information acquisition: acquire or reacquire the applicable conflict area when
       a usable cue is available; head/gaze is a proxy and must have valid tracking. Safe caution without
       acquisition avoids an immediate critical event but does not demonstrate this domain.

   · M2-C3 - Prediction and conflict entry: infer the actor's likely path and adapt speed, braking, and
       commitment so neither actor requires an emergency response.

   · M2-C4 - Pedestrian protection: avoid entry that forces the pedestrian to brake, stop, retreat, or
       evade.

Scan order, cautious delay, extra checks, stop position, false alarms, efficiency, and comfort are
supporting measures. A supporting miss may generate an improvement note but cannot offset a critical
failure or fail an otherwise safe performance by itself. Numerical thresholds for commitment line,
stopping margin, time to collision, and minimum cue exposure remain pilot- and device-calibrated.

The learner passes when valid changed transfer contains no critical failure and all assigned critical
domains have been demonstrated. A legal-rule-only error triggers web remediation and a local-rule
retest without contaminating the universal VR result. Invalid tracking triggers replay or a non-score,
never an automatic failure.

After a first failed transfer, the system gives error-specific feedback, runs one targeted coached repair
with faded support, and presents one new unprompted variant. After a second critical transfer failure,
the module result is not yet demonstrated and the learner is routed to a later reassessment. The system
does not create an unlimited same-session loop and never averages coached success into transfer.

When the learner passes, the street opens toward a route with more variable pavement, grade, parked
vehicles, and limited escape space. The transition message is simple: seeing the conflict early creates
time; the next module asks how to use that time to control speed, surfaces, and space.

                                                                                                                         Page 26
RIDESAFE VR - NARRATIVE

5.12 Cross-team production picture

   · Design: Build three anchor conflict families with multiple safe timing choices and avoid trick cues.
       Treat the stop/signal condition as a local wrapper around the intersection conflict.

   · Engineering: Parameterize actor path, primary cue, visibility fraction, cue onset, occlusion,
       yielding, braking, commitment line, indicator reliability, actor emergency response, and timing.
       Start telemetry before cue exposure.

   · Art: Construct readable occluders and sight lines. Preserve equivalent cue visibility across
       Florida/U.S. and Bogotá packs.

   · Audio: Add spatially accurate but redundant engine, horn, gate, and pedestrian cues.
   · Analytics: Store scenario/device/rule-pack versions, phase, cue availability, tracking validity,

       attention proxy, reacquisition, prediction, speed/brake response, commitment, stopping margin,
       TTC, actor response, pedestrian forced avoidance, legal result, and M2-C1-C4 results separately.
   · QA: Verify no prompts or continuous speed HUD in baseline/transfer; test cautious, aggressive,
       and correct branches; validate scene freezes before discomfort or impact.
   · Legal/content: Bind exact duties to the active rule pack and preserve the portable conflict
       construct.

6. Module 3 narrative - Manage speed, surfaces, and escape space

6.1 Narrative purpose

Module 3 changes the learner's focus from "Is the path clear?" to "How much control margin will remain
if the path changes?" The learner now knows how to detect conflicts, but detection alone is insufficient
when speed, stopping distance, surface, grade, parked vehicles, and escape options interact.
Target learner time: 3-3.5 minutes, with targeted surface or braking repetitions replacing a full-route
replay whenever possible.

The module trains contextual speed rather than devotion to one number. A legal maximum is supplied
by the web layer, but the VR experience demonstrates that a rider may need to travel below that
maximum when sight distance, traction, grade, surface, pedestrians, doors, or route width reduce
margin.

6.2 Environment and entry

The learner enters a longer urban segment that begins predictably and becomes more physically
complex. The route includes a pavement defect, a wet or loose patch, a curb or level transition, a rail or

                                                                                                                         Page 27
RIDESAFE VR - NARRATIVE

tram-track crossing where appropriate to the localization, a short downhill, parked vehicles, and a
blocked or narrowed path.
These are not presented as unrelated mini-games. They unfold as one route where earlier choices affect
later options. Approaching too quickly shortens the time available to recognize a pothole. Swerving
around the pothole can place the rider in the door zone. Entering the door zone can remove the escape
route when an obstruction appears. The narrative therefore makes margin visible through experience
rather than through a lecture.
The Florida/U.S. pack may use cracked pavement, drainage covers, wet leaves or sand, curb ramps, rail
crossings where locally plausible, parked vehicles, construction cones, and a gentle bridge or parking-
ramp descent. The Bogotá pack may include steeper grade, patched pavement, wet conditions, drainage
features, tighter curbs, mixed frontage, delivery activity, and embedded rail or linear surface features
where plausible. Each localization must preserve equivalent detection distance and maneuver demand
for assessment versions.
The opening instruction is minimal: continue to the destination while keeping the vehicle under control
and leaving space to respond. No speed number or ideal path appears during baseline.

6.3 Beat 1 - Establishing contextual speed

The first section is visually open and mechanically simple. It allows the learner to establish a self-
selected cruising speed. Telemetry records speed profile, acceleration, lane or path position, steering
variability, and head orientation. This initial state provides context for later adaptation; the system does
not immediately label it good or bad.
Ahead, the route's sight distance begins to shorten because of a bend, crest, parked vehicle, vegetation,
street furniture, or activity. The first skill is to adapt before the hazard is fully revealed. The learner who
reduces speed because the view is incomplete demonstrates anticipatory margin. The learner who waits
for a fully visible obstacle demonstrates reaction rather than anticipation.
The system records the moment reduced sight distance becomes perceivable, the first speed change,
brake onset, and available stopping distance. This measurement is distinct from whether a collision
ultimately occurs.

6.4 Beat 2 - Pothole or surface defect

A pothole, crack, raised edge, or broken surface lies ahead. Its contrast is natural and calibrated: visible
to an attentive learner with normal or supported visual settings, but not outlined. Precursor information

                                                                                                                         Page 28
RIDESAFE VR - NARRATIVE

may include other riders or vehicles adjusting their line, a water collection, a changed texture, or a
maintenance marker that is present but not conclusive.
The learner has several possible responses. Early detection allows a small speed reduction and
controlled line change with an escape option intact. If stopping is appropriate, progressive braking
should occur before the defect. If avoidance is appropriate, the learner should check available space and
steer smoothly rather than snapping into an adjacent conflict area.
Telemetry records hazard-available time, detection proxy, speed at detection, brake onset, path change
onset, maximum steering rate, lateral displacement, proximity to the defect, and clearance from
adjacent boundaries or actors.
If the learner hits the defect at low or moderate speed, the device produces proportionate haptic, audio,
and visual feedback. If the learner reaches it too fast or with excessive steering, the system produces
stronger vibration, wheel deflection, or a low-energy wobble and may freeze. It does not depict a high-
energy fall. The consequence should reveal that the learner arrived without enough margin, not punish
them for failing to guess the designer's preferred line.

6.5 Debrief 1 - Early detection and response choice

The review shows when the defect became detectable, when the learner changed speed, and where the
chosen path led. If the learner swerved toward another risk, the counterfactual shows how earlier speed
reduction would have created a smaller, more stable correction.
The explanation separates three decisions:

   1. Detect the surface change early.
   2. Reduce speed before demanding steering or braking.
   3. Confirm that the alternative path preserves space.
The system avoids saying "always swerve" or "always brake." The appropriate response depends on
device, speed, surface, surrounding traffic, and available space.

6.6 Beat 3 - Wet or loose surface

The route continues onto a wet patch, loose aggregate, sand, leaves, paint, metal, or another lower-
traction surface appropriate to the locale. Environmental storytelling explains the condition: recent rain,
irrigation, drainage, construction residue, or shade. The surface should not appear as an artificial hazard
tile.

                                                                                                                         Page 29
RIDESAFE VR - NARRATIVE

The learner's strongest action occurs before entering: reduce speed while traction is more reliable,
straighten or simplify the line, and avoid abrupt combined braking and turning on the reduced-traction
section. Device physics must represent the chosen surface consistently enough that repeated practice is
meaningful.

Telemetry records entry speed, brake and steering inputs before and on the surface, wheel slip or
modeled stability state, path deviation, and recovery. If the learner enters too quickly and adds abrupt
steering or braking, the vehicle may slide or wobble within a comfort-safe boundary. The system can
reduce motion or freeze rather than simulate a fall.

6.7 Beat 4 - Curb or level transition

The learner approaches a curb ramp, driveway lip, pavement seam, drainage edge, or level change. The
scene trains recognition, speed adaptation, stable approach, and sufficient angle. The exact feature
depends on the art pack and device model.

The learner should avoid approaching so obliquely that a small wheel can be deflected along the edge.
The system records approach angle, speed, steering stability, contact trajectory, and post-transition
recovery. Coached practice may briefly show an acceptable approach corridor, but baseline and transfer
do not.

6.8 Beat 5 - Rail or linear groove

Where plausible, embedded rail, tram track, expansion joint, drainage channel, or another longitudinal
groove crosses the learner's route. The feature is introduced through normal urban context rather than
as a laboratory obstacle.

The learner must see the groove early, create space, reduce speed, and cross at a stable angle that does
not trap or redirect the wheel. The design must allow adequate room for the maneuver; a scene that
makes the safe action physically impossible is invalid.

Telemetry records first orientation toward the track, speed reduction, approach angle, steering
smoothness, wheel interaction, and recovery. If the learner crosses nearly parallel, the device gives a
directional pull or instability cue. The system freezes if continued simulation would be distressing.

6.9 Beat 6 - Downhill stopping margin

The route descends. The grade is visible through horizon, environment, and device behavior; it is not
communicated only by an icon. A downstream stopping or yielding situation becomes partly visible. The
learner must recognize that the grade changes acceleration and braking demand and begin managing
speed before the final stopping point.

                                                                                                                         Page 30
RIDESAFE VR - NARRATIVE

The e-bike and standing e-scooter respond differently according to their calibrated physics. The system
records free acceleration, throttle state, brake onset, deceleration, stopping error, and stability. The
learning target is not a single braking profile; it is an early, controlled response that preserves traction
and stopping distance.

An unsafe approach produces an overshoot or sharply reduced margin. The scene may place a soft
boundary or nonimpact stop before a pedestrian or vehicle conflict. The debrief compares where
braking began with where it would have needed to begin under the selected device and grade.

6.10 Beat 7 - Parked vehicles and the door zone

At the bottom of the grade, the learner enters a curbside section with parked vehicles. Occupancy cues
vary: a driver silhouette, illuminated brake or interior light, recently stopped vehicle, mirror movement,
wheel position, passenger movement, delivery activity, or a vehicle with no visible occupant. The learner
must not rely on one cue or assume that absence of a cue proves a door will remain closed.

The route geometry offers a locally valid way to preserve clearance. The web rule pack determines the
exact position and passing duty. The VR construct is to scan, reduce speed when uncertainty is high,
maintain lateral clearance, and preserve an escape option.

Telemetry records distance from the door envelope, speed, cue orientation, lateral stability, time spent
within the modeled door zone, and remaining space on the opposite side. If the learner rides too close,
a door may begin opening or an occupant may shift. The event is timed so an earlier position and speed
choice would have prevented the trap. The scene freezes before impact.

6.11 Beat 8 - Blocked lane and escape-space decision

Immediately after or within the parked-vehicle segment, construction, a delivery, waste bin, stopped
vehicle, or pavement work narrows the path. This event combines the whole module. The learner must
detect the obstruction early, check surrounding space, manage speed, choose whether to wait or
reposition, and avoid trading the obstruction for a door or passing conflict.

The environment must permit at least one safe strategy under the active rule pack. The system records
detection, scan, speed change, braking, waiting, repositioning path, minimum clearance, and remaining
escape width. If the learner commits late, a vehicle, door, or boundary reveals the lost option without
forcing a crash.

6.12 Integrated debrief

The review does not replay every bump. It reconstructs the chain that consumed margin. A typical
explanation might show that the learner entered a short-sight-distance section too quickly, detected the

                                                                                                                         Page 31
RIDESAFE VR - NARRATIVE

pothole late, used a sharp swerve, entered the door zone, and then had no escape when the lane
narrowed. The earliest controllable decision - often speed or sight-distance recognition - becomes the
primary repair target.
The visual timeline contains:

   · Hazard or reduced-sight-distance availability.
   · First learner orientation or detection.
   · Speed and braking trace.
   · Path and steering trace.
   · Surface or door-zone entry.
   · Minimum time-to-collision or clearance.
   · Remaining escape space.
The counterfactual shows a less dramatic trajectory: earlier speed adaptation, smaller steering demand,
stable angle, greater parked-car clearance, and a wait or reposition decision before the space
disappears.

6.13 Coached repair

The learner repeats targeted segments. A temporary speed or stopping band may appear before a
hazard. A rail or curb approach corridor may show angle. A translucent door envelope may appear
around parked vehicles. An escape-space overlay may show how one path preserves more options.
These visualizations are explanatory tools, not permanent HUD elements. The first fades after correct
performance. A subsequent repetition preserves only natural environmental cues.

6.14 Changed transfer

The transfer route changes surface contrast, defect location, rail angle, wet-patch extent, grade, parked-
vehicle type, door timing, obstruction, sight distance, and one aspect of device response. It is possible to
use a different streetscape while keeping the skill demand comparable.
The learner receives no speed display or coaching. The system separately scores detection lead time,
contextual speed, braking, angle, stability, clearance, escape space, and recovery.

6.15 Mastery and transition

The learner passes when they detect relevant hazards with enough lead time, adapt speed, use stable
braking and steering, preserve door clearance and escape space, and demonstrate the behavior in the
changed scene. Final thresholds remain device-specific and pilot-calibrated.

                                                                                                                         Page 32
RIDESAFE VR - NARRATIVE

The route then approaches a busier transport and delivery corridor. The transition explains that
preserving space around small obstacles is not enough when large vehicles have blind zones and rear
wheels that follow a different path.

6.16 Cross-team production picture

   · Design: Build the route as a causal chain of margin decisions, not isolated obstacle challenges.
       Ensure safe strategies are physically possible.

   · Engineering: Version traction, wheel, steering, grade, braking, and surface parameters by device.
       Expose hazard contrast, door timing, obstruction, and escape width as controlled variables.

   · Art: Make surfaces and grade naturally readable. Keep hazards plausible for each localization
       without hiding them through noise.

   · Audio/haptics: Differentiate surface texture, wheel deflection, braking, door movement, and
       object proximity; provide alternatives where meaning depends on sound.

   · Analytics: Store cue availability, speed/brake/steer traces, angle, modeled stability, door-zone
       dwell, clearance, escape width, and recovery.

   · QA: Test each surface and device combination, safe and unsafe branches, no-impossible-path
       cases, motion comfort, and disappearance of coaching aids.

   · Legal/content: Web layer supplies maximum speed, access, position, construction, and route
       information; VR never equates the legal maximum with a safe scene speed.

7. Module 4 narrative - Stay out of heavy-vehicle and curbside traps

7.1 Narrative purpose

Module 4 teaches the learner to resist a common and dangerous temptation: moving into a space
beside a large vehicle simply because the gap appears open. The learner must understand that mirrors
do not guarantee visibility, rear wheels cut inward on turns, long bodies sweep across space, buses
move toward and away from the curb, and curbside activity can remove an escape route without
warning.
Target learner time: 2.5-3 minutes, with the large-vehicle turn as the critical scene and bus/delivery
remediation selected only when needed.
The module is not about fearing every truck or bus. It is about reading vehicle state, recognizing when
the rider is becoming hidden or trapped, and choosing to wait, stop behind, or reposition visibly.

                                                                                                                         Page 33
RIDESAFE VR - NARRATIVE

7.2 Environment and entry

The learner enters a corridor with a bus stop, delivery or loading activity, parked vehicles, and an
upcoming intersection or turn. The scene is busier than earlier modules but remains visually structured.
Critical cues - indicator, wheel movement, body angle, mirror relationship, engine change, door activity -
must not be lost in decorative traffic.
The Florida/U.S. pack may use a campus shuttle, city bus, box truck, delivery van, and broad
intersection. The Bogotá pack may use locally appropriate buses, trucks, motorcycles, deliveries,
narrower lanes, steeper grade, and denser curb activity. The large-vehicle geometry and learner decision
space must be calibrated so the same core choices remain possible.
The system says only that the learner should continue through the corridor safely. No blind-zone
overlay, swept-path arc, "do not pass" label, or target stopping point appears during baseline.

7.3 Beat 1 - Bus pull-in

A bus approaches a stop ahead of or beside the learner. Natural cues include speed reduction, indicator,
lateral movement, brake sound, passenger activity, and the relationship between the bus and curb. The
learner must recognize that the corridor beside the bus may narrow and that being level with the bus
can reduce visibility and escape space.
The system records the learner's first orientation to the bus, speed change, relative longitudinal
position, lateral clearance, blind-zone relationship, and decision to pass, wait, or reposition. The active
rule pack determines exact priority and passing legality. The portable skill is to avoid entering a
disappearing space without visibility and margin.
If the learner maintains a safe position and allows the bus movement to resolve, the bus completes the
pull-in normally. If the learner accelerates into the narrowing gap, the bus's lateral movement, tire
sound, indicator, or horn reveals the trap. The system freezes before contact.

7.4 Beat 2 - Bus pull-out

After the stop, the learner encounters the inverse problem. The bus may pull away from the curb as the
learner approaches. The learner must read indicator, wheel angle, engine note, body movement, and
available lane space rather than assuming the bus remains stationary.
Telemetry records cue orientation, speed, braking, accepted gap, relative position, and response. A
locally correct legal decision may vary, but entering a closing lateral gap without sufficient margin
remains an unsafe construct.

                                                                                                                         Page 34
RIDESAFE VR - NARRATIVE

7.5 Beat 3 - Delivery squeeze and curbside door activity

A delivery vehicle or stopped van partially occupies the corridor. Workers, packages, or an open cargo
area signal active curbside use. Parked-vehicle doors and pedestrian movement may be possible. The
learner must avoid focusing on only the obstruction and drifting into another hazard.
The learner can slow, wait, or reposition according to the rule pack and surrounding traffic. The system
records scan toward doors and people, speed adaptation, clearance, waiting, reposition path, and
escape space. If the learner threads the gap too quickly, a door or worker movement reveals the lost
margin and the scene freezes.
This beat deliberately connects Module 3 and Module 4. Generic door-zone skill is not retaught from
zero; it is placed inside a more complex large-vehicle context.

7.6 Beat 4 - Truck preparing to turn

The corridor leads to an intersection where a truck or other long vehicle is positioned to turn across or
alongside the learner's path. The early cue set includes indicator state, cab angle, front-wheel angle,
reduced speed, gap creation, driver head or mirror relationship, and the position of the rear wheels.
The apparent space beside the truck may look inviting. The critical question is whether the learner
enters the inside of the turn or stays behind and visible. The environment must communicate enough
information for prediction without relying on a single perfect indicator.
Telemetry begins before the learner reaches the truck's rear. It records orientation to wheel, cab,
mirror, and indicator regions; speed; relative longitudinal position; entry into modeled blind zones;
blind-zone dwell; inside-pass choice; brake onset; and stopping or repositioning location.
If the learner stays behind, waits, or repositions visibly, the truck completes the turn and the route
clears. The system records independent success.
If the learner enters the inside gap, the truck begins turning. The front moves outward while the rear
wheels cut inward. The visible space tightens. The learner may lose a clear mirror relationship. Engine,
indicator, tire, and horn cues reinforce the visual motion. The system slows or freezes before impact.

7.7 Debrief - The space that disappears

The review begins before the learner entered the gap. It shows the truck's predicted swept path, the
modeled blind-zone region, the learner's trajectory, and the moment the decision became difficult to
reverse. The explanation is not "the truck was bigger." It identifies the mechanics: the rear wheels follow

                                                                                                                         Page 35
RIDESAFE VR - NARRATIVE

an inside path, the body sweeps through space, the driver cannot continuously see every region, and the
learner surrendered both visibility and escape options.
The counterfactual shows the learner stopping behind a visible reference point or repositioning where
mutual visibility and space are greater, as permitted by the rule pack. The truck completes the turn; the
learner proceeds after the path clears.
If the learner already chose correctly, the debrief may be abbreviated to a transparent confirmation of
which cues they used. The system does not make them enter the blind zone just to demonstrate it.

7.8 Coached repair

The scene resets before the truck. A temporary overlay reveals mirror limits and swept path. The learner
can move their head and observe when the virtual rider enters or leaves the modeled blind-zone
relationship. The overlay is explanatory, not a promise that every real vehicle has identical zones.
A coached repetition presents a clear stopping or waiting decision. The overlay fades before the learner
acts. A second repetition changes timing or vehicle and removes the overlay entirely.
The same targeted repair can be applied to bus or delivery events if those were the primary error.
Support should not force every learner through every possible large-vehicle explanation.

7.9 Changed transfer

The transfer scene changes vehicle type or length, turn direction, lane width, curb activity, indicator
timing or reliability, bus movement, delivery obstruction, and actor timing. The learner may face a left-
side or right-side conflict depending on local geometry, but the critical inside-squeeze construct remains
comparable.
No blind-zone or swept-path overlay appears. The system records the first independent
pass/wait/reposition decision and stores transfer separately from coaching.

7.10 Mastery and transition

The learner passes when they avoid critical blind-zone entry, recognize the turning or lateral-movement
cues, choose waiting or repositioning, and maintain clearance in the changed scene. Blind-zone dwell
and inside-pass choice are critical measures; minor route inefficiency does not outweigh a safe decision.
The corridor opens into a calmer destination area. The story appears to be ending, but the final module
changes the frame: safe riding begins before the vehicle moves, and sometimes the safest ride is the
one not taken.

                                                                                                                         Page 36
RIDESAFE VR - NARRATIVE

7.11 Cross-team production picture

   · Design: Make the inside gap plausible without making the scenario a trick. Preserve at least one
       clear safe option and distinguish legal duty from portable hazard recognition.

   · Engineering: Model vehicle wheelbase, rear-wheel path, body sweep, mirror/blind-zone
       approximation, and configurable indicators, timing, and lateral movement.

   · Art: Use locally recognizable buses, trucks, delivery vehicles, curb activity, and mirrors while
       retaining readable wheel and body cues.

   · Audio: Spatialize engine load, indicator, brakes, tires, doors, passenger activity, and horn; keep
       critical meaning redundant.

   · Analytics: Record cue orientation, relative position, blind-zone entry/dwell, inside-pass decision,
       stop/wait/reposition, clearance, TTC, and support.

   · QA: Test correct waiting, legal alternate paths, early and late passes, indicator variants, different
       vehicle lengths, and freeze-before-impact behavior.

   · Localization: Update vehicle fleet and road context without presenting one country's bus or
       passing law as universal.

8. Module 5 narrative - Choose whether and how to ride

8.1 Narrative purpose

Module 5 is the culmination because it asks the learner to regulate themself rather than simply respond
to the road. The sequence begins before motion. The learner faces social pressure, an explicit alcohol,
drug, or severe-fatigue condition, and realistic alternatives. If they make the safe no-ride decision, the
module treats that choice as mastery rather than denying them content. If they ride under a condition
that remains permissible within the scenario, the route tests phone, carrying, low light, glare,
pedestrians, and shared-space behavior. If they choose to ride despite a defined no-ride condition, the
system does not glamorize impairment; it safely reveals the decision chain and routes them to repair.

Target learner time: 2.5-3 minutes. A successful no-ride choice must not create a punitive time penalty;
remaining skill coverage should use a clearly separate fit-to-ride occasion or transfer branch.

8.2 A new time and emotional tone

The scene shifts to later in the day or a separate ride opportunity. The learner is outside a social,
workplace, campus, hospitality, or community setting. The destination matters: home, transit,
accommodation, or another safe endpoint. The environment includes people but avoids caricature or
moral judgment.

                                                                                                                         Page 37
RIDESAFE VR - NARRATIVE

Lighting is dusk or early night. The scene remains accessible and visually legible. Low-light difficulty
increases through realistic contrast, glare, and sight distance rather than simply darkening the display.
The learner has access to their micromobility device and at least one alternative such as transit, a ride
from another person, taxi or ride-hail, walking where appropriate, waiting, or contacting help.
Alternatives must be plausible for the deployment context and presented without advertising bias.

8.3 Beat 1 - The ride/no-ride decision

A narrative cue establishes the learner's condition. The scenario may state that the learner consumed
alcohol, used an impairing drug or medication as explicitly defined by the content owner, or is severely
fatigued after extended wakefulness or work. The cue must be unambiguous enough that the training
does not become a guessing game about whether the impairment "counts."

A peer or contextual prompt creates mild social pressure: "It's close," "Everyone else is leaving," or "You
have your scooter." The tone remains natural and never mocks refusal. The learner sees the device and
alternatives. No "correct answer" glow appears.

The system records which information the learner inspects, decision time, ride/no-ride choice, response
to social pressure, and alternative selected.

If the learner refuses to ride and chooses a viable alternative, the system confirms the reasoning: the
safe skill was recognizing the condition before motion and selecting another plan. The learner does not
have to simulate riding while impaired. To preserve training coverage, phone, low-light, and shared-
space content can appear in a short follow-up ride framed as a different, fit-to-ride occasion, or can be
delivered through a transfer variant. The product team must select this branch strategy before
production so no-ride success is not punished with a longer session.

If the learner chooses to ride despite the explicit no-ride condition, the system pauses before vehicle
activation. It does not apply wavy visuals, exaggerated controls, or entertainment effects. A debrief
shows that the earliest controllable decision was made before the trip. It presents the available
alternatives and the consequences or resources from the active web rule pack. The learner then
practices selecting the safe alternative. Any later riding content is reframed as a different occasion when
the learner is fit to ride.

8.4 Debrief 1 - Self-regulation

The debrief distinguishes risk evidence from legal consequence. It explains that impairment and severe
fatigue can compromise judgment, response, and control, while the exact offense and enforcement
information is jurisdiction-specific. The system links to verified web content with current source and
effective date.

                                                                                                                         Page 38
RIDESAFE VR - NARRATIVE

The learner is asked to choose a concrete alternative. The system positively records refusal, decision
time, and alternative selection. The outcome summary should make clear that not starting is successful
performance.

8.5 Beat 2 - Load security and continuous control availability

On a fit-to-ride occasion, the learner prepares to leave with a small object, bag, food container, package,
or other plausible item. Secure storage is available. The learner decides whether to secure it or hold it.
If the object is secured, the route continues without drama. If it is held or hung where it obstructs
control, the learner selects that configuration through the mounted interface and the system records
the unsafe carrying decision. During initial motion, the simulated object may shift and the configured
control demand may change. This is a modeled consequence of the selected configuration, not evidence
that the system measured a physical hand leaving the handlebar. The learner can stop and choose
secure storage.
The instructional point is not that every carried object is illegal. It is that the presented method
compromises control. Exact passenger and load rules remain in the web layer.

8.6 Beat 3 - Notification and stop-before-phone behavior

After the learner establishes motion, a realistic notification arrives through audio, vibration, or display
according to the device setup. The notification is intentionally nonemergency. With both controllers still
fixed, the learner uses head/gaze focus and a reachable mounted confirmation input to choose whether
to ignore it, stop in a safe location before requesting interaction, or request interaction while moving.
Telemetry records notification time, valid attention proxy, interaction request, whether the request
occurred while moving or after a safe stop, speed change, path deviation, stopping location, and time to
resume. Hand release and one-hand duration are absent unless a later rig includes independently
validated grip or hand sensing.
If the learner ignores the notification or stops safely before interacting, the system records the choice
and continues. It does not interrupt with praise that breaks immersion.
If the learner requests interaction while moving, the route introduces a normal demand - a bend,
surface change, pedestrian cue, or narrowing - not a contrived ambush. The learner may drift, react late,
or reduce clearance. The system produces an early warning or freezes before a conflict. Debrief shows
that the earliest safe decision was to defer or stop, not to become better at glancing quickly.

                                                                                                                         Page 39
RIDESAFE VR - NARRATIVE

8.7 Beat 4 - Low-light adaptation

The route darkens naturally. Streetlights, storefronts, vehicle headlights, shadows, reflections, and glare
create changing contrast. The learner's own required equipment state is configured according to the
active rule pack, but the scene's portable construct is detection and adaptation.
A distant pedestrian, cyclist, vehicle, surface change, or crossing cue becomes visible gradually. The
learner must reduce speed when sight distance falls, direct attention farther ahead, manage glare, and
preserve stopping margin. The system records speed before and after the light change, calibrated visible
distance, valid attention-proxy acquisition when available, speed/brake response, and recovery after
glare. It does not claim eye fixation when only head pose is available.
The scene must not rely on display darkness that disadvantages certain headsets or users unpredictably.
Calibration and accessibility modes preserve the construct while increasing contrast or providing
equivalent cues when required.
If the learner maintains a daytime speed despite reduced sight distance, the event reveals the lost
margin through late detection or sharp braking. The world freezes before impact. The debrief compares
visible stopping distance with the learner's approach and shows how earlier speed adaptation created
time.

8.8 Beat 5 - Shared space and pedestrian interaction

The learner enters a mixed or shared environment only if permitted by the active rule pack; otherwise
an equivalent pedestrian-rich conflict occurs on a locally valid facility. The environment includes
pedestrians moving at different speeds, a child or group only if ethical and comfort review approves, a
person with mobility or visual-access needs, and potential opposing users. The scene must not use
vulnerable people as shock props.
The learner needs to slow, yield, communicate when useful, and pass with space. Communication may
include bell, voice, or another locally appropriate method, but no single method is universally required
by the VR layer. The system records speed, valid attention proxy where available, yield, communication,
passing side where locally relevant, minimum clearance, and pedestrian response.
The safest outcome may be to wait. The scoring model must not reward speed or completion time at the
expense of space. If a pedestrian has to stop abruptly or change path because the learner passes too
fast or close, that response becomes the consequence. The scene freezes before contact.

                                                                                                                         Page 40
RIDESAFE VR - NARRATIVE

8.9 Integrated debrief

The review returns to the earliest self-regulation decision that affected the route. If the learner chose to
ride under an explicit no-ride condition, that decision remains primary. If the learner was fit to ride but
used a phone while moving, the phone decision is primary. If attention was appropriate but low-light
speed remained high, speed adaptation becomes primary.
The timeline can contain:

   · Ride/no-ride cue and selected alternative.
   · Load state and hand availability.
   · Notification and interaction.
   · Light or glare change.
   · First pedestrian or hazard cue.
   · Speed, braking, path, yield, and clearance.
The counterfactual is deliberately ordinary: choose another mode, secure the item, ignore the phone or
stop safely, reduce speed as visibility falls, detect the pedestrian earlier, and wait or pass with space.
The product avoids heroics.

8.10 Coached repair

Repair begins with the earliest failed decision. A learner who chose to ride while unfit practices choosing
an alternative rather than practicing impaired control. A learner who used the phone while moving
practices a safe stopping sequence. A learner who failed low-light adaptation receives a temporary
visible-sight-distance or stopping-margin explanation. A learner who passed a pedestrian too closely
receives a temporary clearance corridor and prediction prompt.
Supports fade after correct action. A short integrated repetition combines the repaired behaviors
without overlays.

8.11 Changed transfer

The transfer scenario changes social pressure, impairment or fatigue cue, alternative availability, object
and storage, notification timing, lighting, glare source, pedestrian path, route density, and facility type.
The no-ride construct remains explicit rather than ambiguous.
The learner again makes the earliest decision without prompts. If they do not ride, the system records
successful transfer of self-regulation. If they ride in a fit-to-ride variant, the system records phone/load
behavior, low-light adaptation, yield, communication, and clearance separately.

                                                                                                                         Page 41
RIDESAFE VR - NARRATIVE

8.12 Mastery and session close

The learner must demonstrate a safe ride/no-ride decision under the presented condition and
acceptable unprompted attention, low-light, and shared-space behavior in the relevant branches.
Knowledge points cannot compensate for a critical unsafe decision.

The experience ends in a calm summary environment. The report presents:

   · Skills demonstrated independently.
   · Skills demonstrated only with support.
   · Transfer performance, kept separate from coaching.
   · Any required remediation or retest.
   · One or two priority behaviors rather than an overwhelming list.
   · Links to the selected jurisdiction's legal, equipment, route, parking, impairment, and help

       information.
   · An explanation that VR performance is not a guarantee of future real-world safety.
If a delayed booster is part of the deployment, the learner is told when and why it will occur. The session
ends without a crash montage or inflated claim.

8.13 Cross-team production picture

   · Design: Treat no-ride as a successful interactive outcome. Ensure later content remains available
       without making refusal feel punitive. Keep social pressure realistic and nonjudgmental.

   · Engineering: Build clean branch state between no-ride, alternative selection, and a separate fit-to-
       ride continuation. Log notifications, hand state, lighting, glare, pedestrian, yield, and clearance
       events.

   · Art: Create believable social and nighttime contexts with accessible contrast. Avoid stereotypes
       and sensationalized impairment visuals.

   · Audio: Use natural social dialogue, notification, traffic ambience, glare-adjacent vehicle sound,
       bell/voice communication, and redundant accessible cues.

   · Analytics: Keep ride/no-ride, alternative, notification decision, moving-versus-stopped interaction
       request, modeled load state, night adaptation, valid attention proxy, yield, and clearance as
       separate measures. Do not report hand release without a validated sensor.

   · QA: Test refusal success, unsafe selection and repair, every alternative, phone branches, secure-
       load branches, lighting calibrations, accessibility modes, and no-impact freezes.

   · Legal/content: Version impairment law, equipment, facility access, speed, pedestrian duties, and
       help resources independently of the VR build.

                                                                                                                         Page 42
RIDESAFE VR - NARRATIVE

9. End-to-end adaptive flow

The complete experience should not behave as five identical loops. Module 1 is a selection-and-
demonstration lesson, the control-familiarization gate is explicit instruction, and Modules 2-5 use the
active baseline/repair/transfer loop. Adaptation keeps the session efficient and humane:

   1. In Module 1, every learner submits the safety-element set and the device-specific checklist before
       seeing the corresponding answer video. Critical misunderstandings become later retrieval targets
       rather than identical immediate retests.

   2. A learner who clears the control-familiarization actions proceeds quickly; a learner who struggles
       receives one coached retry or facilitator/setup support before scored traffic.

   3. In Modules 2-5, a learner who performs safely in baseline receives a short confirmation, little or
       no coaching, and still completes changed transfer.

   4. A learner who makes one targeted error receives one targeted explanation and repair, not a
       generic replay of the full module.

   5. A learner who reaches a critical safety or comfort boundary is paused before impact and given a
       calm review.

   6. A failed first transfer receives targeted remediation and one new unprompted transfer. A second
       critical failure is recorded as not yet demonstrated and routed to later reassessment rather than
       immediate repetition.

   7. A learner who chooses not to ride under an explicit impairment condition is credited with
       successful performance and is never forced to simulate impairment.

   8. Accessibility settings remain active across modules and are stored with scoring context.
   9. Legal-rule changes can alter text, valid routes, signs, and rule evaluation without changing the

       core perception/control metric.

10. Global production state model

The implementation should expose a common state structure while allowing the Module 1 and
familiarization exceptions:

State                     Required system behavior
Pre-module configuration
Module 1 selection armed  With controllers handheld, select and persist language, location/rule pack, vehicle, accessibility,
                          comfort, direct-interaction mapping, and scenario version

                          Keep controllers handheld; present neutral item/component options; hide correctness; record
                          learner focus and choices

                                   Page 43
RIDESAFE VR - NARRATIVE          Required system behavior
                                 Compare learner and reference sets; play applicable corrective and reference clips; assign later
  State                          retrieval targets
  Module 1 instructional review  Disable propulsion; instruct learner to request help; require facilitator controller mounting,
                                 retention/tracking, rider-position, reach, and neutral-steering checks
  Passthrough mount lock         Teach the mounted controls; record diagnostic performance; require minimum start/turn/both-
                                 brake stop and recenter operation before independent traffic assessment
  Control familiarization        Load the selected device physics, localization, rule pack, accessibility, comfort, and scenario
                                 version
  Active-module setup            Remove instructional overlays; start pre-cue telemetry; present only natural goal and controls
  Baseline armed                 Timestamp cue availability and actor/environment state
  Critical cue available         Record orientation, control, position, clearance, and decision events
  Learner response               Preserve normal world response; record independent success; do not force consequence
  Safe resolution                Trigger only from defined unsafe state; cap intensity; freeze before impact or distress
  Managed consequence            Select earliest material cue/decision; replay; show counterfactual; separate legal note
  Debrief                        Enable only targeted supports and log their level
  Coached repair                 Remove supports according to criterion; never label prompted behavior independent
  Fade                           Load changed variables; remove supports; store separate transfer score
  Transfer armed                 Apply critical gate, targeted remediation, one new transfer, later reassessment, or progression
  Module resolution              logic
  Handoff                        Save evidence IDs, scene/rule/device versions, accessibility context, results, and next action

11. Shared data and event expectations

Names are conceptual and should be translated into the implementation's event schema. Every critical
scene needs events for:

   · Session, learner pseudonymous ID, module, scene, build, device, hardware, physics, localization,
       rule-pack, language, and accessibility versions.

   · Pre-module language/location/vehicle focus, selection, confirmation, changes, and persistence
       state.

   · Module 1 item/component focus, selection, deselection, order, submission, comparison result,
       clip exposure, replay, and later-retrieval target.

   · Control-familiarization activation, acceleration, steering, braking, stop, pause, assistance, retry,
       and facilitator-intervention state.

                                 Page 44
RIDESAFE VR - NARRATIVE

   · Baseline, coaching level, faded attempt, transfer, and retention phase.
   · Cue availability, cue type, location, actor state, and visibility or occlusion state.
   · Head-orientation or gaze proxy with hardware capability clearly marked.
   · Speed, acceleration, brake, steering, position, path, stability model, and stopping state.
   · Clearance, TTC, accepted gap, blind-zone relationship, door-zone relationship, and escape width.
   · Start/no-start, ride/no-ride, yield, entry, pass, wait, reposition, phone, load, and alternative

       decisions.
   · Consequence trigger, type, intensity, freeze reason, and comfort stop.
   · Debrief target, counterfactual shown, support enabled, repetition count, and support faded.
   · Mastery result, critical error, remediation, transfer result, retention result, and dropout or

       sickness reason.
Raw telemetry retention, privacy, consent, and access controls require a separate data-governance
specification. The training report should use the minimum information necessary and avoid presenting
uncertain gaze proxies as direct cognitive measurement.

12. Narrative acceptance checklist

Before a module is considered implemented according to this blueprint, reviewers should be able to
answer yes to all applicable questions:

   · Does the learner understand the natural goal without being shown the scoring answer?
   · Can every pre-module and Module 1 interaction be completed handheld from a safe stationary

       reach envelope, and can all later interactions be completed without removing either mounted
       controller?
   · Does the passthrough transition prevent propulsion, instruct the learner to request facilitator
       help, and verify both mounts, tracking, rider position, reachable controls, and neutral steering
       before familiarization?
   · Does Module 1 preserve the learner's submitted choices, pinpoint omissions or unsafe selections,
       and then demonstrate the complete correct set without pretending to measure physical riding
       skill?
   · Does the control-familiarization gate occur before the first scored traffic conflict, and is its
       telemetry separated from Module 1?
   · Is the baseline free of instructional prompts, points, target bands, and continuous speed display?
   · Are the critical cues visible and plausible in every supported localization and accessibility mode?

                                                                                                                         Page 45
RIDESAFE VR - NARRATIVE

   · Can the learner make more than one reasonable safe timing choice where appropriate?
   · Does every unsafe consequence result from a defined learner state?
   · Does the system avoid forcing a crash or failure after safe behavior?
   · Does the debrief identify the earliest controllable cue or decision?
   · Is the counterfactual concise, accurate, and compatible with the active rule pack?
   · Is coaching targeted, logged, and removed before independent mastery?
   · Does transfer change at least two material variables when feasible?
   · Are baseline, coaching, transfer, and retention results stored separately?
   · Are critical errors prevented from being offset by trivial points?
   · Are device physics and thresholds versioned by hardware?
   · Are legal claims sourced from the web rule pack rather than hard-coded?
   · Are seated/standing, handedness, captions, audio alternatives, comfort, pause, and early-exit

       behaviors tested?
   · Can QA reproduce every safe, unsafe, remediation, refusal, and comfort-stop branch?
   · Does the final learner report remain inside the evidence claim boundary?

13. Decisions that remain intentionally open

This narrative makes the experience concrete without pretending that unresolved production decisions
are settled. The following still require named owners and due dates:

   1. Approve or revise the proposed evidence-based safety-element taxonomy in §§4.5.1 and 4.8.1-
       4.8.3, then validate its device-specific fault examples against the selected reference e-scooter and
       e-bike.

   2. Choose whether Module 1's changed-example retrieval occurs at session end, inside a later VR
       module, or in the web booster.

   3. Approve the two-transfer-attempt limit and practice/observation progression rule after a second
       critical failure.

   4. Approve Module 2's three-baseline/two-transfer structure within 3-3.5 minutes, or require all
       three transfer anchors and revise its target to approximately 4-5 minutes.

   5. Approve the maximum total headset time and adaptive-video length after low-fidelity testing.
   6. Exact launch hardware and the physics/threshold calibration protocol for each device.
   7. Confirm whether any launch rig includes eye tracking or validated grip/hand sensing; otherwise

       preserve the head-orientation and no-hand-state claim limits.

                                                                                                                         Page 46
RIDESAFE VR - NARRATIVE

   8. Approve the final mounting supports, facilitator procedure, passthrough wording, readiness
       confirmation input, and mounted freeze-and-predict control after checking reach on both rigs.

   9. Exact Bogotá learner profile and deployment environment.
   10. Rule-pack behavior when local law is ambiguous, changing, or conflicts across city, county,

       operator, and facility layers.
   11. Final scenario budgets, mastery composites, and critical thresholds for Modules 3-5 after their

       focused evidence gates.
   12. Comparator, randomization, sample size, and delayed-retention budget.
   13. Launch accessibility requirements and threshold implications.
   14. Data retention, privacy, consent, and reporting governance.
   15. Decide whether hand signaling at launch is facilitator-observed, represented through a

       dedicated mounted input, or deferred until validated hand tracking is available.
Until those decisions are approved, the narrative defines required behavior and relationships, not final
numeric constants.

Appendix A - Module element inventories

These inventories make the narrative scannable for production planning. They describe required
functional categories, not a final asset count. Reuse is encouraged when it does not make transfer
scenes visually or behaviorally identical.

A.1 Module 1 - Safety elements and vehicle readiness

Category                    Required elements
Pre-module UI
Spaces                      Language, location, and vehicle cards; handheld point/confirm/back; persistent selected state;
Devices                     accessibility options; direct-interaction rehearsal
Personal-safety assortment
                            Localized mobility hub or garage; personal-safety display; vehicle-diagnostic bay; comfort-tested
Vehicle components          automatic transition or fade; passthrough mounting state after Module 1
Learner interaction
                            Standing e-scooter and e-bike visual variants; component targets; normal, adjustable,
                            maintenance, and critical-fault reference states

                            P1-P8 from §4.5.1: correct and defective/poor-fit helmets; secure and obstructive clothing/laces;
                            condition-dependent conspicuity; optional-neutral pads/gloves/eyewear; secure and
                            handheld/handlebar-hung load configurations

                            V1-V6 from §4.8.1 plus the scooter package in §4.8.2 or e-bike package in §4.8.3: brakes;
                            tires/wheels; steering/load-bearing structure; conditional visibility/warning devices; electric
                            status/controls; and fault disposition

                            Handheld point/focus; inspect; take/place or tag; select/deselect; review choices; submit with
                            confirmation; no riding or claim of real tactile inspection

                                                      Page 47
RIDESAFE VR - NARRATIVE       Required elements

  Category                    Learner/reference comparison; complete appropriate set; correct-use clips; omitted-core clips;
  Safety feedback assets      inappropriate-choice risk clips; captions and audio alternatives
  Diagnostic feedback assets
  Analytics                   Learner/reference checklist; component inspection close views; correct/faulty comparisons; road-
  Later retrieval variables   ready, adjust, maintenance, remove-from-service, and alternate-vehicle interpretations
  Web handoff
                              Item/component focus and selection history; omissions; inappropriate selections; submission
                              latency; clip exposure/replay; post-review confirmation; later-retrieval target

                              Different item examples, different correct/faulty component images, altered presentation order,
                              or a web microlearning booster; never the identical immediate board

                              Helmet/age; passenger/load; lights/equipment; class; wattage; assist/throttle; licensing,
                              registration, title, insurance

A.1.1 Control-familiarization gate before Module 2

Category                      Required elements
Space
Hardware                      Mixed-reality/passthrough mounting state followed by a protected, low-clutter, non-traffic lane with start,
                              straight path, stopping target, gentle turn, and safe pause state
Instructional cues
Completion evidence           Left/right secured controller supports; mounted handlebar; valid tracking; reachable activation,
Failure handling              acceleration, both brakes, steering, confirm, pause, emergency-stop, and recenter inputs; facilitator
Data boundary                 access

                              Request-facilitator and mounting animation; neutral-alignment cue; activation cue; smooth acceleration
                              cue; steering guide; progressive both-brake guide; stop target; gentle-turn guide; required look-check;
                              pause/recenter

                              Mount/tracking validity; rider position; neutral steering; activation; controlled movement; broad turn;
                              progressive both-brake stop; pause/recenter; no facilitator takeover

                              Repeat one failed variable; return several failures to static mapping; after two assisted sequences,
                              setup/off-course practice/end-session intervention; no scored traffic entry if minimum operation is not
                              achieved

                              Diagnostic context only; separate from Module 1 selections and from Module 2 independent performance

A.2 Module 2 - See and be seen at conflicts

Category                      Required elements
Scenario budget
Spaces                        Three baseline anchors; one selective diagnosed repair; two changed transfer encounters from
Occluders                     different critical families; use 4-5 minutes if all three anchors must repeat in transfer

                              Protected-lane exit; occluded garage/driveway/alley; locally governed intersection with
                              approaching or turning conflict; pedestrian crossing; destination path

                              Wall, hedge, parked van, shelter, sign, street furniture, landscaping, gate, or equivalent localized
                              elements

                                                    Page 48
RIDESAFE VR - NARRATIVE     Required elements

  Category                  Emerging vehicle; approaching or turning vehicle; pedestrian; optional cyclist or motorcycle transfer
  Actors                    actor
  Primary scored cues
  Corroborating cues        Partial actor/wheel visibility; wheel rotation/angle; vehicle/body motion; speed/lateral-position
  Redundant-only cues       change; gap closure; pedestrian posture/movement
  Learner options
  Conditional consequences  Indicator/light state; readable driver body orientation; spatial engine, gate, tire, or pedestrian audio
  Fixed-handlebar response
  Debrief assets            Shadow/reflection or subtle lighting change; never the sole required cue
  Coaching assets
  Transfer variables        Slow; expose sight line; scan; stop/yield; wait; choose locally legal visible position; enter after
  Critical behaviors        clearance; protect pedestrian
  Supporting measures
                            Vehicle braking; horn; shortened gap; pedestrian hesitation or stop; freeze before impact
  Required telemetry
                            Head/gaze-directed focus plus accessible mounted trigger/button for freeze-and-predict; no
  Transfer resolution       controller removal or free-hand pointing

  Web handoff               Occluded-region view; sight-line visualization; cue timestamp; attention-proxy/path trace;
                            speed/brake/commitment trace; error pinpoint; safer counterfactual

                            Diagnosis-specific freeze-and-predict prompt, sight-line boundary, first-cue marker, or
                            stopping/commitment reference; support fades within repair and is absent in transfer

                            Actor; occluder; geometry; approach side; turn direction; signal phase; indicator reliability; timing;
                            light; pedestrian path

                            M2-C1 margin under occlusion; M2-C2 relevant-information acquisition/reacquisition; M2-C3
                            prediction and conflict-entry decision; M2-C4 pedestrian protection

                            Scan order; cautious delay; extra checks; stop position; false alarms; efficiency; clearance; comfort

                            Scenario/device/rule-pack/phase; cue ID and availability; visibility fraction; tracking validity;
                            attention-proxy acquisition/reacquisition; prediction; speed response; brake onset; speed at
                            commitment; commitment line; stopping margin; TTC; actor emergency response; pedestrian
                            forced avoidance; legal result; support level; M2-C1-C4

                            No critical failure and all assigned critical domains demonstrated; one error-specific repair and one
                            new variant after first failure; not yet demonstrated after second critical transfer failure; invalid
                            tracking is replay/non-score

                            Exact right-of-way; stop/dismount; turns; crossings; lane/facility position; sidewalk/path access;
                            signs and markings

A.3 Module 3 - Manage speed, surfaces, and escape space

Category                    Required elements
Spaces
Surface assets              Open approach; shortened sight line; defect segment; lower-traction segment; level transition;
Curbside assets             rail/groove crossing; downhill; parked-car corridor; blocked/narrowed path

                            Pothole/crack/raised edge; wet patch; sand/leaves/aggregate; paint/metal where appropriate;
                            curb/ramp/seam; rail, joint, or drainage groove

                            Occupied and unoccupied parked vehicles; door envelopes; delivery/construction/waste obstruction;
                            safe waiting or repositioning space

                                                         Page 49
RIDESAFE VR - NARRATIVE     Required elements

  Category                  Surface contrast; pooled water; changed texture; maintenance marker; other actor line change; grade
  Natural cues              horizon; interior/brake light; silhouette; mirror/occupant movement
  Learner options
  Conditional consequences  Reduce speed; progressive brake; stop; smooth avoidance; stable angle; wait; reposition; preserve
  Debrief assets            door clearance and escape width
  Coaching assets
  Transfer variables        Vibration; haptic texture; wheel deflection; modeled slip; low-energy wobble; overshoot; opening
  Web handoff               door; closing escape gap; freeze

                            Cue/detection markers; speed/brake trace; path/steering trace; angle; stability state; door-zone
                            dwell; TTC; escape-width visualization

                            Temporary speed/stopping band; stable approach corridor; rail/curb angle corridor; door envelope;
                            escape-space overlay

                            Contrast; defect location; traction; rail angle; curb geometry; grade; door timing; obstruction; sight
                            distance; device response

                            Maximum speed; facility access; lane position; slow zones/geofences; construction; closures; route
                            conditions

A.4 Module 4 - Stay out of heavy-vehicle and curbside traps

Category                    Required elements
Spaces
Vehicles                    Bus stop; travel corridor; delivery/loading area; parked-car edge; intersection or turn; visible
Curbside actors             waiting/repositioning area
Natural cues
Learner options             Local bus/shuttle; long truck or equivalent; delivery van/box vehicle; parked vehicle; configurable
Conditional consequences    vehicle lengths and wheelbases
Debrief assets
Coaching assets             Passenger activity; driver; delivery worker; pedestrian; optional motorcycle/cyclist context that does
Transfer variables          not obscure the primary cue
Web handoff
                            Indicator; wheel angle; body angle; mirror relationship; brake/engine/tire sound; lateral movement;
                            passenger activity; door/cargo movement

                            Slow; remain behind; wait; stop; avoid inside pass; reposition visibly; preserve lateral and longitudinal
                            clearance

                            Narrowing gap; mirror loss; lateral encroachment; rear-wheel cut-in; horn; door/worker movement;
                            freeze before impact

                            Approximate blind-zone visualization; swept path; front/rear wheel paths; learner relative position;
                            blind-zone dwell; lost escape space

                            Mirror-limit overlay; swept-path overlay; temporary stopping/reposition target; faded explanation
                            states

                            Vehicle type/length; turn direction/radius; indicator timing/reliability; lane width; curb activity; bus
                            movement; obstruction; timing

                            Passing; lane position; bus/bike-lane priority; loading/curb rules; facility access; signs; parking

                                                             Page 50
RIDESAFE VR - NARRATIVE

A.5 Module 5 - Choose whether and how to ride

Category                  Required elements
Spaces
Narrative actors          Social/work/campus/community departure; device and alternatives; secure-load point; fit-to-ride
Decision cues             route; dusk/night transition; pedestrian-rich or shared-space segment; calm summary environment
Learner options
Conditional consequences  Peer or social-pressure character; transport/help alternative; pedestrians with varied paths and
Debrief assets            access needs; ordinary traffic actors
Coaching assets
Transfer variables        Explicit alcohol/drug/severe-fatigue state; destination; plausible alternatives; device availability;
Web handoff               nonemergency notification; object/load; lighting/glare change
Summary outputs
                          Refuse ride; choose alternative; secure load; ignore phone; stop safely before interaction; adapt night
                          speed/scan; yield; wait; communicate; pass with space

                          Activation pause for unsafe no-ride decision; modeled object shift or control/attention demand; late
                          response; reduced night margin; pedestrian hesitation/stop; freeze before conflict

                          Ride/no-ride decision chain; alternative selection; hand/load state; notification timeline; light/sight-
                          distance change; speed/path/yield/clearance traces

                          Alternative-selection rehearsal; safe-stop sequence; load-security cue; temporary
                          sight-distance/stopping-margin view; pedestrian clearance corridor

                          Social pressure; impairment/fatigue cue; alternative availability; object/storage; notification time;
                          light/glare; pedestrian path; facility; route density

                          Impairment law and help; lights/equipment; operating access; pedestrian duties; speed; helmet/age;
                          passenger/load; alternatives

                          Independent skills; supported skills; changed-transfer result; remediation; web links; retention
                          schedule; explicit claim boundary

                                               Page 51

```
