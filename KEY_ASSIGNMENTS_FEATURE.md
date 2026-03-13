# Key Assignments Feature - Implementation Summary

## Overview
A new feature has been added to the RedRat3 Controller CLI that allows users to:
1. View all keyboard shortcuts and their assigned IR commands
2. Edit/change the key bindings for any command
3. Save key assignments to a file (persistence)
4. Validate key assignments to prevent duplicate keys
5. Display error messages in Action History when duplicate keys are detected

## New Files Created

### 1. KeyAssignment.cs
Simple class representing a key-to-signal mapping:
- `SignalName`: The internal name of the IR signal
- `DisplayName`: User-friendly name for the command
- `AssignedKey`: The keyboard key assigned to this command

### 2. KeyAssignmentsWindow.cs
A Windows Form dialog that provides:
- DataGridView showing all key assignments
- Click on any key cell and press a new key to reassign
- Save button to persist changes to `key_assignments.xml`
- Cancel button to discard changes
- Validation to prevent duplicate key assignments
- Automatic loading of previously saved assignments

## Modified Files

### RedRat3ControllerForm.cs
Key changes:
- Replaced static `KeyToIRSignalMap` dictionary with dynamic `keyAssignments` list
- Added `LoadKeyAssignments()` method to load saved assignments on startup
- Added `RebuildKeyMap()` to rebuild the key mapping dictionary after changes
- Modified `SignalOutput()` to check for duplicate key assignments
- Changed `ShowCommandsButton_Click` to open the KeyAssignmentsWindow instead of showing a read-only list
- Added event handler to receive updates from the KeyAssignmentsWindow

## How It Works

### Startup
1. Application loads key assignments from `key_assignments.xml`
2. If file doesn't exist, uses default assignments
3. Builds the `KeyToIRSignalMap` dictionary from loaded assignments

### Viewing/Editing Key Assignments
1. Click the "Show Commands" button
2. KeyAssignmentsWindow opens showing all commands and their assigned keys
3. Click on any cell in the "Assigned Key" column
4. Press a new keyboard key to change the assignment
5. Click "Save" to persist changes or "Cancel" to discard

### Saving
1. When "Save" is clicked, the window validates all key assignments
2. Checks for duplicate keys (same key assigned to multiple commands)
3. If duplicates found, shows error dialog and prevents saving
4. If validation passes, saves to `key_assignments.xml`
5. Notifies main form to update its key mapping
6. Main form rebuilds its key map and logs the change

### Runtime Validation
1. When a key is pressed, the system checks if it's assigned to multiple commands
2. If duplicate assignment detected:
   - Shows error message box
   - Logs error to Action History
   - Does NOT execute any command
3. If unique assignment, executes the normally assigned command

## Persistence
- Key assignments are saved in `key_assignments.xml` in the application directory
- Format: XML with `KeyAssignments` root and `KeyAssignment` child elements
- File is automatically created on first save
- Changes persist across application restarts

## Example key_assignments.xml
```xml
<KeyAssignments>
  <KeyAssignment>
    <SignalName>DTV_POWER_K</SignalName>
    <DisplayName>Power Key</DisplayName>
    <AssignedKey>P</AssignedKey>
  </KeyAssignment>
  <KeyAssignment>
    <SignalName>DTV_VOLUME_UP_K</SignalName>
    <DisplayName>Volume Up</DisplayName>
    <AssignedKey>Multiply</AssignedKey>
  </KeyAssignment>
  <!-- ... more assignments ... -->
</KeyAssignments>
```

## Error Handling
- **Duplicate Key Detection**: Prevents the same key from being assigned to multiple commands
- **Error Display**: Shows both a message box popup and logs to Action History
- **Safe Defaults**: If file loading fails, falls back to default assignments
- **No Action Execution**: Commands are not executed when duplicate keys are detected

## Benefits
1. **Customization**: Users can personalize their keyboard shortcuts
2. **Persistence**: Changes are saved and remembered between sessions
3. **Validation**: Prevents accidental duplicate assignments
4. **User-Friendly**: Simple click-to-edit interface
5. **Error Visibility**: Clear error messages in both popup and Action History
6. **Safety**: Cancel option allows previewing changes without committing

## Testing
To test the feature:
1. Run the application
2. Click "Show Commands" button
3. Try changing some key assignments (e.g., change Power from 'P' to 'K')
4. Click "Save" - should save and update main form
5. Test pressing the new key (should trigger the command)
6. Try assigning the same key to multiple commands (should show error)
7. Close and reopen application (changes should persist)