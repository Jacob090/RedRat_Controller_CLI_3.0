using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RedRat3ControllerCLI
{
    public class KeyAssignmentsWindow : Form
    {
        private List<KeyAssignment> keyAssignments;
        private DataGridView dataGridView;
        private Button saveButton;
        private Button cancelButton;
        private Label titleLabel;
        private const string SettingsFile = "key_assignments.xml";

        public event EventHandler<KeyAssignmentsUpdatedEventArgs>? KeyAssignmentsUpdated;

        public KeyAssignmentsWindow(List<KeyAssignment> assignments)
        {
            keyAssignments = assignments;
            InitializeComponent();
            LoadDataToGrid();
        }

        private void InitializeComponent()
        {
            this.Text = "Key Assignments Editor";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Title Label
            titleLabel = new Label();
            titleLabel.Text = "Edit Key Assignments";
            titleLabel.Font = new Font("Arial", 14, FontStyle.Bold);
            titleLabel.Location = new Point(20, 20);
            titleLabel.Size = new Size(400, 30);
            this.Controls.Add(titleLabel);

            // DataGridView
            dataGridView = new DataGridView();
            dataGridView.Location = new Point(20, 60);
            dataGridView.Size = new Size(740, 450);
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.ReadOnly = false;
            dataGridView.EditMode = DataGridViewEditMode.EditOnEnter;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.MultiSelect = false;
            dataGridView.AllowUserToOrderColumns = false;
            dataGridView.RowHeadersVisible = false;
            dataGridView.BackgroundColor = Color.White;
            dataGridView.BorderStyle = BorderStyle.FixedSingle;
            
            // Add columns
            dataGridView.Columns.Add("SignalName", "Signal Name");
            dataGridView.Columns.Add("DisplayName", "Display Name");
            dataGridView.Columns.Add("AssignedKey", "Assigned Key");
            
            // Column settings
            dataGridView.Columns[0].ReadOnly = true;
            dataGridView.Columns[1].ReadOnly = true;
            dataGridView.Columns[2].ReadOnly = false;
            dataGridView.Columns[0].Width = 200;
            dataGridView.Columns[1].Width = 200;
            dataGridView.Columns[2].Width = 200;
            
            this.Controls.Add(dataGridView);

            // Save Button
            saveButton = new Button();
            saveButton.Text = "Save";
            saveButton.Location = new Point(580, 520);
            saveButton.Size = new Size(90, 35);
            saveButton.BackColor = Color.LightGreen;
            saveButton.Click += SaveButton_Click;
            this.Controls.Add(saveButton);

            // Cancel Button
            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(680, 520);
            cancelButton.Size = new Size(90, 35);
            cancelButton.BackColor = Color.LightCoral;
            cancelButton.Click += CancelButton_Click;
            this.Controls.Add(cancelButton);

            // Help Label
            Label helpLabel = new Label();
            helpLabel.Text = "Click on a key in the 'Assigned Key' column and press a new key to change it.";
            helpLabel.Location = new Point(20, 525);
            helpLabel.Size = new Size(550, 30);
            helpLabel.ForeColor = Color.Gray;
            this.Controls.Add(helpLabel);
        }

        private void LoadDataToGrid()
        {
            dataGridView.Rows.Clear();
            foreach (var assignment in keyAssignments)
            {
                dataGridView.Rows.Add(
                    assignment.SignalName,
                    assignment.DisplayName,
                    assignment.KeyToString()
                );
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            // Check if we're editing a cell in the Assigned Key column
            if (dataGridView.EditingControl != null && 
                dataGridView.CurrentCell != null && 
                dataGridView.CurrentCell.ColumnIndex == 2)
            {
                // Convert the key to string and update the cell
                string keyName = GetKeyName(e.KeyCode);
                dataGridView.CurrentCell.Value = keyName;
                dataGridView.EndEdit();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private string GetKeyName(Keys key)
        {
            // Handle special keys
            switch (key)
            {
                case Keys.D0: return "D0";
                case Keys.D1: return "D1";
                case Keys.D2: return "D2";
                case Keys.D3: return "D3";
                case Keys.D4: return "D4";
                case Keys.D5: return "D5";
                case Keys.D6: return "D6";
                case Keys.D7: return "D7";
                case Keys.D8: return "D8";
                case Keys.D9: return "D9";
                case Keys.Oemcomma: return "Oemcomma";
                case Keys.OemPeriod: return "OemPeriod";
                default:
                    return key.ToString();
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            // Validate key assignments for duplicates
            var validationErrors = new List<string>();
            var usedKeys = new Dictionary<Keys, string>();

            for (int i = 0; i < dataGridView.Rows.Count; i++)
            {
                var row = dataGridView.Rows[i];
                string signalName = row.Cells[0].Value?.ToString() ?? "";
                string displayName = row.Cells[1].Value?.ToString() ?? "";
                string keyString = row.Cells[2].Value?.ToString() ?? "";

                var key = KeyAssignment.StringToKey(keyString);
                if (!key.HasValue)
                {
                    validationErrors.Add($"Invalid key for {displayName}: {keyString}");
                    continue;
                }

                // Check for duplicate keys
                if (usedKeys.ContainsKey(key.Value))
                {
                    validationErrors.Add($"Key '{keyString}' is assigned to both '{displayName}' and '{usedKeys[key.Value]}'");
                }
                else
                {
                    usedKeys[key.Value] = displayName;
                }

                // Update the key assignment
                keyAssignments[i].AssignedKey = key.Value;
            }

            // Show errors if any
            if (validationErrors.Count > 0)
            {
                string errorMessage = "Validation errors found:\n\n" + string.Join("\n", validationErrors);
                MessageBox.Show(errorMessage, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Save to file
            try
            {
                SaveKeyAssignmentsToFile();
                
                // Raise event to notify main form
                KeyAssignmentsUpdated?.Invoke(this, new KeyAssignmentsUpdatedEventArgs(keyAssignments));
                
                MessageBox.Show("Key assignments saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving key assignments: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void SaveKeyAssignmentsToFile()
        {
            var doc = new XDocument(
                new XElement("KeyAssignments",
                    keyAssignments.Select(ka =>
                        new XElement("KeyAssignment",
                            new XElement("SignalName", ka.SignalName),
                            new XElement("DisplayName", ka.DisplayName),
                            new XElement("AssignedKey", ka.AssignedKey.ToString())
                        )
                    )
                )
            );

            doc.Save(SettingsFile);
        }

        public static List<KeyAssignment> LoadKeyAssignmentsFromFile()
        {
            var assignments = GetDefaultKeyAssignments();

            if (!File.Exists(SettingsFile))
            {
                return assignments;
            }

            try
            {
                var doc = XDocument.Load(SettingsFile);
                var root = doc.Element("KeyAssignments");
                if (root != null)
                {
                    var loadedAssignments = root.Elements("KeyAssignment").ToList();
                    
                    foreach (var loaded in loadedAssignments)
                    {
                        string signalName = loaded.Element("SignalName")?.Value ?? "";
                        string keyString = loaded.Element("AssignedKey")?.Value ?? "";
                        
                        var key = KeyAssignment.StringToKey(keyString);
                        if (key.HasValue)
                        {
                            // Find matching assignment and update its key
                            var assignment = assignments.FirstOrDefault(a => a.SignalName == signalName);
                            if (assignment != null)
                            {
                                assignment.AssignedKey = key.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If loading fails, return defaults
                System.Diagnostics.Debug.WriteLine($"Error loading key assignments: {ex.Message}");
            }

            return assignments;
        }

        private static List<KeyAssignment> GetDefaultKeyAssignments()
        {
            return new List<KeyAssignment>
            {
                new KeyAssignment("DTV_POWER_K", "Power Key", Keys.P),
                new KeyAssignment("DTV_SOURCE_K", "Source Key", Keys.H),
                new KeyAssignment("DTV_0_K", "0", Keys.D0),
                new KeyAssignment("DTV_1_K", "1", Keys.D1),
                new KeyAssignment("DTV_2_K", "2", Keys.D2),
                new KeyAssignment("DTV_3_K", "3", Keys.D3),
                new KeyAssignment("DTV_4_K", "4", Keys.D4),
                new KeyAssignment("DTV_5_K", "5", Keys.D5),
                new KeyAssignment("DTV_6_K", "6", Keys.D6),
                new KeyAssignment("DTV_7_K", "7", Keys.D7),
                new KeyAssignment("DTV_8_K", "8", Keys.D8),
                new KeyAssignment("DTV_9_K", "9", Keys.D9),
                new KeyAssignment("DTV_TTX_MIX_K", "TTX/Mix Key", Keys.T),
                new KeyAssignment("DTV_MUTE_K", "Mute Key", Keys.V),
                new KeyAssignment("DTV_CH_LIST_K", "Channel List Key", Keys.C),
                new KeyAssignment("DTV_VOL_UP_K", "Volume Up", Keys.Multiply),
                new KeyAssignment("DTV_VOL_DOWN_K", "Volume Down", Keys.Divide),
                new KeyAssignment("DTV_CH_UP_K", "Channel Up", Keys.Add),
                new KeyAssignment("DTV_CH_DOWN_K", "Channel Down", Keys.Subtract),
                new KeyAssignment("DTV_MENU_K", "Menu Key", Keys.M),
                new KeyAssignment("DTV_SMART_K", "Smart Key", Keys.S),
                new KeyAssignment("DTV_GUIDE_K", "Guide Key", Keys.G),
                new KeyAssignment("DTV_UP_K", "Up Arrow", Keys.Up),
                new KeyAssignment("DTV_DOWN_K", "Down Arrow", Keys.Down),
                new KeyAssignment("DTV_LEFT_K", "Left Arrow", Keys.Left),
                new KeyAssignment("DTV_RIGHT_K", "Right Arrow", Keys.Right),
                new KeyAssignment("DTV_ENTER_K", "Enter Key", Keys.Enter),
                new KeyAssignment("DTV_INFO_K", "Info Key", Keys.I),
                new KeyAssignment("DTV_RETURN_K", "Return Key", Keys.Back),
                new KeyAssignment("DTV_EXIT_K", "Exit Key", Keys.X),
                new KeyAssignment("DTV_RED_K", "Red Key (F1)", Keys.F1),
                new KeyAssignment("DTV_GREEN_K", "Green Key (F2)", Keys.F2),
                new KeyAssignment("DTV_YELLOW_K", "Yellow Key (F3)", Keys.F3),
                new KeyAssignment("DTV_BLUE_K", "Blue Key (F4)", Keys.F4),
                new KeyAssignment("SPE_MORE_K", "More Key (Q)", Keys.Q),
                new KeyAssignment("DTV_REWIND_K", "Rewind", Keys.Oemcomma),
                new KeyAssignment("DTV_FF_K", "Fast Forward", Keys.OemPeriod),
                new KeyAssignment("DTV_PAUSE_K", "Pause Key", Keys.U),
                new KeyAssignment("DTV_REC_K", "Record Key", Keys.R),
                new KeyAssignment("DTV_PLAY_K", "Play Key", Keys.Y),
                new KeyAssignment("DTV_STOP_K", "Stop Key", Keys.Z),
                new KeyAssignment("DTV_FACTORY_K", "Factory Key", Keys.F),
            };
        }

        public static List<KeyAssignment> GetCurrentKeyAssignments()
        {
            return LoadKeyAssignmentsFromFile();
        }
    }

    public class KeyAssignmentsUpdatedEventArgs : EventArgs
    {
        public List<KeyAssignment> KeyAssignments { get; }

        public KeyAssignmentsUpdatedEventArgs(List<KeyAssignment> assignments)
        {
            KeyAssignments = assignments;
        }
    }
}