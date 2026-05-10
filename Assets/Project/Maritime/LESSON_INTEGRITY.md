# Lesson Integrity — 2026-05-10 11:26

## State machine
- on `MaritimeLessonRoot`
- title: `Bridge Familiarization & Departure`
- phases configured: 5

## Phases
- [0] ✓ Briefing | title `Briefing` | manualAdvance=True | objectives: 0
- [1] ✓ Familiarization | title `Familiarization` | manualAdvance=False | objectives: 4
     - AisPowerObjective `Power on the AIS Class A transceiver`
     - VhfPowerObjective `Power on the VHF radio (Channel 16 watch)`
     - EcdisPowerObjective `Power on the ECDIS chart display`
     - RadarTransmitObjective `Switch the radar to Transmit`
- [2] ✓ Guided | title `Guided procedure` | manualAdvance=False | objectives: 2
     - TelegraphPositionObjective `Set the engine telegraph to Slow Ahead (right hand)`
     - HelmHeadingObjective `Steer to course 045°T (left hand)`
- [3] ✓ Assessment | title `Assessment` | manualAdvance=False | objectives: 1
     - EventObjective `Hold VHF Distress on Channel 16 for 3 seconds`
- [4] ✓ Debrief | title `Debrief` | manualAdvance=True | objectives: 0

## WrongHandPenaltyTracker
- present

## UI views
- LessonHUDView: present
- BriefingView:  present
- DebriefView:   present

## Total issues: **0**
