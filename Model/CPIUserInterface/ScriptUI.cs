﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;

using ApsimFile;
using Controllers;
using CSGeneral;
using CMPServices;

namespace CPIUserInterface
{
    

    //=====================================================================
    /// <summary>
    /// This UI is used to support the AusFarm Manager component. It contains
    /// a basic syntax highlighter. APSIM must have do_management event
    /// published from clock in the first part of the day.
    /// </summary>
    //=====================================================================
    public partial class ScriptUI : CPIBaseView
    {
        //settings for the manager script highlighting
        private String[] keywords = { "define", "on", "on_event", "each", "from", "to", "repeat", 
                                          "days", "months", "years", "start", "if", "else", "for", 
                                          "step", "while", "set", "reset", "true", "false", "const",
                                          "volatile", "real", "integer", "text", "string", "logical",
                                          "subroutine", "call"};
        private String[] operators = {"and", "or", "mod", "div", "not", "exp", "ln", "sin", "cos", 
                                             "atan", "abs", "round", "floor", "max", "min", "sum", 
                                             "average", "upper", "lower", "str", "length"};

        private Font normalFont = new Font("Courier New", 10, FontStyle.Regular);
        private Color normalColor = Color.Black;
        private Font commentFont = new Font("Courier New", 10, FontStyle.Italic);
        private Color commentColor = Color.Red;
        private Font keyFont = new Font("Courier New", 10, FontStyle.Bold);
        private Color keyColor = Color.Blue;
        private Font opFont = new Font("Courier New", 10, FontStyle.Bold);
        private Color opColor = Color.Maroon;
        private Regex r = new Regex("([ \\t{}():;])");
        private Regex rLines = new Regex("\\n");

        private Boolean FILLING;
        private List<TTypedValue> typedvals;
        /// <summary>
        /// Constructor
        /// </summary>
        public ScriptUI()
        {
            InitializeComponent();
            typedvals = new List<TTypedValue>();
            label1.Text = "Ln 1 Col 1";
        }
        protected override void OnLoad()
        {
            base.HelpText = " Manager script";
        }
        //=====================================================================
        /// <summary>
        /// Load on creation and find the dll (manager)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //=====================================================================
        private void ScriptUI_Load(object sender, EventArgs e)
        {
            //find the full path to the dll for the component (normally use InitFromComponentDescription() )
            String ComponentType = Controller.ApsimData.Find(NodePath).Type;
            List<String> DllFileNames = Types.Instance.Dlls(ComponentType);
            FDllFileName = DllFileNames[0];
            FDllFileName = Configuration.RemoveMacros(FDllFileName);
        }
        //=======================================================================
        /// <summary>
        /// Initialise the list of properties with values from the init section
        /// SDML. 
        /// </summary>
        /// <param name="initXML">The init section which is <code><initsection>...</initsection></code></param>
        //=======================================================================
        private Boolean InitFromInitSection(String initXML)
        {
            if (initXML.Length > 0)
            {
                typedvals.Clear();
                TInitParser initPsr = new TInitParser(initXML);

                for (int i = 1; i <= initPsr.initCount(); i++)
                {
                    String initText = initPsr.initText((uint)i);
                    TSDMLValue sdmlinit = new TSDMLValue(initText, "");
                    typedvals.Add(sdmlinit);

                    if (sdmlinit.Name == "rules")
                    {
                        StringBuilder str = new StringBuilder();
                        for (uint line = 1; line <= sdmlinit.count(); line++)
                        {
                            str.Append(sdmlinit.item(line).asStr() + "\n");
                        }
                        Highlight(str.ToString());
                    }
                    if (sdmlinit.Name == "logfile")
                    {
                        textBox1.Text = sdmlinit.asStr();
                        checkBox1.Checked = (textBox1.Text.Length > 0);
                    }
                    if (sdmlinit.Name == "log_set")
                    {
                        
                    }
                }
            }
            return (initXML.Length > 0); 
        }
        //=====================================================================
        /// <summary>
        /// Save the changes on the form.
        /// </summary>
        //=====================================================================
        public override void OnSave()
        {
            StoreControls();
            String newXML = WriteInitsectionXml();

            //now store the new xml by replacing the old xmlnode in Data
            XmlNode initSection = XmlHelper.Find(Data, "initsection");
            if (initSection != null)
                Data.RemoveChild(initSection);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(newXML);
            Data.AppendChild(Data.OwnerDocument.ImportNode(doc.DocumentElement, true));
        }
        //=====================================================================
        /// <summary>
        /// Write the TTypedValues to an xml string.
        /// </summary>
        /// <returns>The init section string</returns>
        //=====================================================================
        protected override String WriteInitsectionXml()
        {
            StringBuilder newXML = new StringBuilder();
            newXML.Append("<initsection>");

            TSDMLValue sdmlWriter = new TSDMLValue("<init/>", "");
            for (int i = 0; i < typedvals.Count; i++)
            {
                newXML.Append(sdmlWriter.getText(typedvals[i], 0, 2));
            }
            newXML.Append("</initsection>");
            return newXML.ToString();
        }
        //=====================================================================
        /// <summary>
        /// Store the forms controls into the list of TTypedValues.
        /// </summary>
        //=====================================================================
        private void StoreControls()
        {
            for (int i = 0; i < typedvals.Count; i++)
            {
                TTypedValue sdmlinit = typedvals[i];
                if (sdmlinit.Name == "rules")
                {
                    sdmlinit.setElementCount((uint)richTextBox1.Lines.Length);
                    for (uint line = 0; line < richTextBox1.Lines.Length; line++)
                    {
                        sdmlinit.item(line + 1).setValue(richTextBox1.Lines[line]);
                    }
                }
                if (sdmlinit.Name == "logfile")
                {
                    if (checkBox1.Checked)
                        sdmlinit.setValue(textBox1.Text);
                    else
                        sdmlinit.setValue("");
                }
                if (sdmlinit.Name == "log_set")
                {
                    
                }
            }
        }
        //=======================================================================
        /// <summary>
        /// Refill the interface with the values for this instance.
        /// </summary>
        //=======================================================================
        public override void OnRefresh()
        {
            base.OnRefresh();

            //Fill the property fields
            XmlNode initSection = XmlHelper.Find(Data, "initsection");

            if (initSection != null)
            {
                InitFromInitSection(initSection.OuterXml);
            }
        }
        //=======================================================================
        /// <summary>
        /// Get the new log file name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //=======================================================================
        private void button1_Click(object sender, EventArgs e)
        {
            //set the path to the log file
            if (textBox1.Text.Length > 0)
                saveFileDialog1.FileName = textBox1.Text;

            saveFileDialog1.Filter = "Log files (*.log)|*.log|All files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                textBox1.Text = saveFileDialog1.FileName;
            }
        }
        //=======================================================================
        /// <summary>
        /// Highlight a given line
        /// </summary>
        /// <param name="lineNumber"></param>
        //=======================================================================
        private void HighlightALine(int lineNumber)
        {
            if (richTextBox1.Lines.Length > 0)
            {
                FILLING = true;

                LockWindow(richTextBox1.Handle);
                int cursorPos = richTextBox1.SelectionStart;
                int startOfLine = richTextBox1.GetFirstCharIndexFromLine(lineNumber);
                String strLine = richTextBox1.Lines[lineNumber];

                //now select the text I am about to parse
                richTextBox1.SelectionStart = startOfLine;
                richTextBox1.SelectionLength = strLine.Length;

                ParseLine(strLine); //redraws the text with highlighting

                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionStart = cursorPos;

                LockWindow(IntPtr.Zero);
                richTextBox1.Update();

                FILLING = false;
            }
        }
        [DllImport("user32.dll")]
        public static extern bool LockWindowUpdate(IntPtr hWndLock);
        /// <summary>
        /// Stop the internal contents of the richtextbox from redrawing
        /// </summary>
        /// <param name="hWndLock"></param>
        private void LockWindow(IntPtr hWndLock)
        {
            //mono-linux alternative to stopping the redrawing of the text - rather rough but stops the crashing
            if (Path.VolumeSeparatorChar == '/')
                richTextBox1.Visible = (hWndLock == IntPtr.Zero);
            else
                LockWindowUpdate(hWndLock);
        }
        //=======================================================================
        /// <summary>
        /// Sets the text of the richedit to text value. Uses a very simple
        /// syntax highlighter.
        /// </summary>
        /// <param name="text">Text to highlight</param>
        //=======================================================================
        private void Highlight(String text)
        {
            FILLING = true;
            richTextBox1.Clear();
            richTextBox1.Font = normalFont;
            richTextBox1.SelectionStart = 0;
            richTextBox1.Enabled = false;
            String[] lines = rLines.Split(text);    //adds an extra \n
            int Count = lines.Length - 1;
            for (int i = 0; i < Count; i++ )
            {
                ParseLine(lines[i]);
                if (i < Count -1)
                    richTextBox1.SelectedText = "\n";
            }
            richTextBox1.Enabled = true;
            richTextBox1.Select(0, 0);
            FILLING = false;
        }
        //=======================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        //=======================================================================
        void ParseLine(string line)
        {
            FILLING = true;
            richTextBox1.SuspendLayout();
            String[] tokens = r.Split(line);
            Boolean comment = false;
            Boolean keywordFound = false;
            foreach (string token in tokens)
            {
                keywordFound = false;
                if ((token.Length > 0) && (token[0] == '!')) //check for a comment
                {
                    comment = true;
                    richTextBox1.SelectionColor = commentColor;
                    richTextBox1.SelectionFont = commentFont;
                    keywordFound = true;
                }
                if ((!comment) && (token.Length > 0) && (token[0] != ' '))
                {
                    // Check whether the token is a keyword. 
                    for (int i = 0; i < keywords.Length; i++)
                    {
                        if (keywords[i] == token)
                        {
                            // Apply alternative color and font to highlight keyword.
                            richTextBox1.SelectionColor = keyColor;
                            richTextBox1.SelectionFont = keyFont;
                            keywordFound = true;
                            break;
                        }
                    }
                    if (!keywordFound)
                    {
                        for (int i = 0; i < operators.Length; i++)
                        {
                            if (operators[i] == token)
                            {
                                // Apply alternative color and font to highlight operator.
                                richTextBox1.SelectionColor = opColor;
                                richTextBox1.SelectionFont = opFont;
                                keywordFound = true;
                                break;
                            }
                        }
                    }
                }
                if (!keywordFound && !comment)
                {
                    if (richTextBox1.SelectionColor != normalColor)
                        richTextBox1.SelectionColor = normalColor;
                    if (richTextBox1.SelectionFont != normalFont)
                        richTextBox1.SelectionFont = normalFont;
                }
                richTextBox1.SelectedText = token;
            }
            richTextBox1.ResumeLayout();
        }
        //=======================================================================
        /// <summary>
        /// Need to re check this line as it is edited.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //=======================================================================
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (!FILLING)
                HighlightALine(richTextBox1.GetLineFromCharIndex(richTextBox1.SelectionStart));
        }
        //=======================================================================
        private void richTextBox1_SelectionChanged(object sender, EventArgs e)
        {
            // update the caret position if no text is selected
            if (richTextBox1.SelectionLength == 0) 
                UpdateCaretPos();
        }
        //=======================================================================
        /// <summary>
        /// Rewrites the line number and column number of the cursor on the label.
        /// </summary>
        //=======================================================================
        private void UpdateCaretPos()
        {
            int index, line;
            Point pt;
            index = richTextBox1.SelectionStart;
            line = richTextBox1.GetLineFromCharIndex(index);
            // get the caret position in pixel coordinates
            pt = richTextBox1.GetPositionFromCharIndex(index);
            index = richTextBox1.GetCharIndexFromPosition(pt);

            // now get the character index at the start of the line, and
            // subtract from the current index to get the column
            pt.X = 0;
            int col = index - richTextBox1.GetCharIndexFromPosition(pt);
            label1.Text = "Ln " + (++line).ToString() + " Col " + (++col).ToString();
        }
        //=======================================================================
        /// <summary>
        /// Open help file based on the dll component name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //=======================================================================
        private void button2_Click(object sender, EventArgs e)
        {
            //find the full path to the dll for the component
            String ComponentType = Controller.ApsimData.Find(NodePath).Type;
            List<String> DllFileNames = Types.Instance.Dlls(ComponentType);
            String DllFileName = DllFileNames[0];
            DllFileName = Configuration.RemoveMacros(DllFileName);
            String helpFileName = Path.ChangeExtension(DllFileName, "chm");
            if (File.Exists(helpFileName))
            {
                openHelp(helpFileName);
            }
            else {
                helpFileName = Path.ChangeExtension(DllFileName, "html");
                if (File.Exists(helpFileName))
                {
                    openHelp(helpFileName);
                }
                else
                    MessageBox.Show("Cannot find help file " + helpFileName);
            }
        }
        //=========================================================================
        /// <summary>
        /// Opens the component's initialising dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //=========================================================================
        private void btnEdit_Click(object sender, EventArgs e)
        {
            StoreControls();
            String sInitSDML = WriteInitsectionXml(); //with no subscribed events in the SDML
            if (Initialise(FDllFileName, ref sInitSDML))
                InitFromInitSection(sInitSDML);
        }
        
    }
}
