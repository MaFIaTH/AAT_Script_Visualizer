# AAT Script Visualizer
**Ace Attorney Trilogy - Script Visualizer** (and somewhat a script editor as well)

**This tool supports the following:**
1. Visualizing text and related commands such as Wait, SetTextColor, SetMessageTime, SetTextFlag, etc.
2. Modifying the value of some commands.
3. Removal of some commands.
4. Addition of some commands.
5. Save your progress as a project file to come back later.
6. Export your progress back as a .txt file.

## Disclaimer:
I recently just taught myself WPF to make this application, if you have a suggestion or can help me improve, please do not hesitate. You can fork and create a pull request if you like.

## Notice:
Even though this tool can edit script files to a reliable extent, however, **it is not supposed to be used as a translation/localization tool!**

If you want more simplicity for translating just the text command then please consider using one of my tools, [AAT Text Extractor](https://github.com/MaFIaTH/AAT_TextExtractor).

This tool is supposed to be treated as a proofreader with editing options for fixing formatting and mistakes that were made by previous translation workflow.

## Usage:
**Prerequisite:**

Make sure that the script files (both original and translated) are already converted to .txt (might support auto-conversion for later updates).

**Usage:**
1. Download the latest pre-build release and extracted the file to your working directory.

2. Launch the tool and go to File -> New Project...

3. Select the original file, translated file, and project directory.

4. If everything was done correctly, the list view on the right-hand side should expand and display commands.

5. For visualization, you can either click Play for an automatic playthrough or click Next/Previous for a manual playthrough, or double-click at any item on the list view to skip to the selected part of the script.

6. For editing, you can right-click on any item on the list view and select appropriate actions. (Some actions are unavailable to some commands, visit [Wiki][Wiki] for more detail.)

7. For saving, go to File -> Save/Save As... to save your project.

8. For exporting, go to File -> Export As... then select your export directory.

## For More Details:
- Visit [Wiki][Wiki] for more information/usage/description.
- Visit [FAQ][FAQ] for questions regarding usage/bugs/known problems.

[FAQ]: https://github.com/MaFIaTH/AAT_Script_Visualizer/wiki/FAQ
[Wiki]: https://github.com/MaFIaTH/AAT_Script_Visualizer/wiki
