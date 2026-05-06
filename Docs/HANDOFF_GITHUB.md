# Maritime Bridge VR LMS Handoff

## Current Goal

Build a desktop-simulated VR training scene for a maritime engine telegraph lesson. The player uses mouse/keyboard as a development substitute for a real headset/controller setup.

## Project Entry Points

- Unity project: `E:\Sach\Sua\test\VR\Unity-FirstHand`
- Unity version used here: `6000.4.4f1`
- Main scene: `Assets/Project/Scenes/MaritimeBridgeLMS.unity`
- Earlier sandbox scene: `Assets/Project/Scenes/Level/DesktopGrabSandbox.unity`

## Work Completed

- Created a maritime bridge lesson scene with ship/bridge/sea/telegraph training objects.
- Added desktop VR controller logic under `Assets/Project/Scripts/DesktopMockVR/`.
- Added keyboard hand selection:
  - `1`: left hand
  - `2`: right hand
  - `3`: both hands
- Implemented "magic hands" behavior: selected hand(s) move toward the object under the mouse ray instead of acting as an invisible cursor.
- Added hand visibility/priority helpers so hands remain legible over objects.
- Added telegraph interaction logic through `DesktopLeverInteractable`.
- Added a specialized telegraph grip path so held lever poses use `leftHandGripPose` / `rightHandGripPose`.
- Adjusted current hand orientation target so thumbs face back toward the player/camera more like two palms facing each other.

## Current Known State

- Unity was closed before the final hand-orientation pass could be visually rechecked in Play Mode because multiple Unity processes/projects were causing severe lag.
- The latest code compiles conceptually and previous console checks were clean, but the final hand orientation and telegraph grip pose should be verified in Play Mode by the next developer.
- The worktree contains many Unity-generated and Unity-AI-generated changes. For a clean handoff branch, commit a full snapshot of project assets but keep local `.tmp_*`, `.agents`, `.codex`, `.claude`, `.vscode`, and generated solution files out of git.

## Recommended GitHub Handoff Flow

Do not push directly to `origin/main`. `origin` currently points at the upstream sample repo:

```powershell
origin https://github.com/oculus-samples/Unity-FirstHand.git
```

Use a private repo or fork instead:

```powershell
cd E:\Sach\Sua\test\VR\Unity-FirstHand
git switch -c handoff/maritime-bridge-lms
git add -A
git status --short
git commit -m "Build maritime bridge desktop VR prototype"
gh repo create meiiie/maritime-bridge-lms --private --source=. --remote=handoff --push
```

If using a fork of the original sample instead:

```powershell
gh repo fork oculus-samples/Unity-FirstHand --remote --remote-name myfork
git push -u myfork handoff/maritime-bridge-lms
```

## Next Developer Checklist

- Open `Assets/Project/Scenes/MaritimeBridgeLMS.unity`.
- Test hand selection with `1`, `2`, `3`.
- Verify the hand orientation against the reference: thumbs should be on the player/camera side, with palms facing inward.
- Test telegraph hover/grab/release.
- Tune `Telegraph Left Hand Grip Pose` and `Telegraph Right Hand Grip Pose` under the telegraph pivot if the hand still does not sit naturally on the handle.
- Profile Play Mode before adding more assets. The current project can lag if multiple Unity instances or AI Toolkit processes are open.
