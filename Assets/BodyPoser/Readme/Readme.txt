BodyPoser version 1.4 – Quick Pose Editing, Scene Dressing & Batch Posing Tool

This package includes the BodyPoser system, designed for fast and intuitive posing of humanoid models directly in the Unity Editor. It is ideal for setting up static corpses, scene dressing, cinematic moments, or decorative character placement – without requiring animations or complex runtime physics setups.

As of the latest update, the package also includes a **Batch Pose Tool Editor Window**, allowing you to efficiently manage and operate on large groups of posed characters.

───────────────────────────────────────
📦 INCLUDED

* BodyPoser.cs (Main runtime + editor script)
* BodyPoseBatchTool.cs (Optional scene-based batch component)
* BodyPoseBatchToolWindow.cs (Editor Window for batch operations)
* Demo Scene (Example setup with pre-posed models and one ragdoll for testing)

───────────────────────────────────────
🛠 PREPARATION

In the demo scene you can immediately use the included demo model.

For your own models:

* Your character should have a standard humanoid or bone hierarchy
* (Optional) A ragdoll setup if you want to use physics-based posing

───────────────────────────────────────
🧍 BODY POSER – BASIC USAGE

1. Attach the **BodyPoser** script to any humanoid character or ragdoll.

2. There are two ways to pose models:

   2.1 **Ragdoll Posing**

   * Ensure the model has a ragdoll (Rigidbodies + Colliders)
   * Enter Play Mode and let physics settle the body
   * When satisfied, proceed to step 3

   2.2 **Editor Pose Editing**

   * Enable **Pose Edit Mode** in the Inspector
   * Manipulate bones directly in the Scene View using handles
   * When satisfied, proceed to step 3

3. Use the Inspector buttons:

   * 📸 Capture Current Pose to JSON
     (For ragdoll poses – capture during Play Mode, then exit Play Mode)
   * 📐 Apply Stored Pose from JSON (Editor Mode)
   * 🗑️ Clear Stored JSON Pose
   * ⚙️ Remove Physics Components (recommended for static scene dressing)

4. Optional: Enable **Disable Physics On Start** if you want to keep physics components but freeze them at runtime.

Tips:

* For maximum performance: remove physics components and mark the GameObject as static.

───────────────────────────────────────
📦 BATCH POSE TOOL – OVERVIEW

The Batch Pose Tool allows you to perform pose-related operations on **multiple BodyPoser objects at once**.

It consists of two parts:

1. **BodyPoseBatchTool (Component)**

   * A scene-based component that stores a persistent list of BodyPoser targets
   * Useful if you want saved target lists per scene or runtime options (e.g. disable physics on start)

2. **Batch Pose Tool Editor Window (Recommended Workflow)**

   * A dedicated editor window that stays open while you select objects
   * Designed to efficiently handle large selections (dozens or hundreds of objects)

───────────────────────────────────────
🪟 BATCH POSE TOOL – EDITOR WINDOW (RECOMMENDED)

Open the window via:

Tools / RevolvingGearStudios / Body Poser / Batch Pose Tool

The window provides:

* A reference field to an existing BodyPoseBatchTool
* Buttons to **Find** or **Create** one in the scene
* Selection-based tools that do NOT require the batch tool to be selected

Core features:

* Replace From Selection
* Add From Selection
* Optional **Include Children** toggle
* Cleanup (remove nulls & duplicates)
* Select current targets in the Hierarchy

Batch actions:

* Capture all poses
* Apply all stored poses
* Remove all physics components
* Make all targets static

Typical workflow:

1. Open the Batch Pose Tool window
2. Click **Find In Scene** or **Create In Scene**
3. Select any number of objects in the Hierarchy
4. Use **Add From Selection** or **Replace From Selection**
5. Run batch actions as needed

This workflow avoids Unity’s selection limitations and is the preferred way to batch large groups.

───────────────────────────────────────
📖 IMPORTANT WORKFLOW NOTES

* If you are **only posing static models for scene decoration**, you do NOT need to capture poses.
  Simply pose the model in the editor and save the scene.

* Use **Capture Pose** only when:

  * You want to preserve a ragdoll pose created by physics
  * You want to re-apply poses later or at runtime

* Captured poses are stored as JSON files in:
  Assets/BodyPoser/TempPoseData/

  These files are intended for temporary or intermediate use unless explicitly needed for gameplay.

───────────────────────────────────────
📧 SUPPORT

Contact: [revolvinggearstudios@gmail.com](mailto:revolvinggearstudios@gmail.com)

Join our Discord:
[https://discord.gg/g5fK7Df](https://discord.gg/g5fK7Df)

Thank you for using BodyPoser – and happy scene dressing!
