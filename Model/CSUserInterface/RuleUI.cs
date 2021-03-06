using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;

using System.Collections.Specialized;
using System.IO;
using System.Xml;

using Controllers;
using CSGeneral;
using UIBits;
namespace CSUserInterface
{
	//InputDialog

	//nb. this overloads Generic UI. This RuleUI is just GenericUI with a Script tab added on, so the user can also write their own management scripts.

	public partial class RuleUI : BaseView
	{
		internal GenericUI GenericUI;
		public RuleUI() : base()
		{

			//This call is required by the Windows Form Designer.
			InitializeComponent();

			//Add any initialization after the InitializeComponent() call

		}

        protected override void OnLoad()
		{
		}
		// -----------------------------------
		// Refresh the UI
		// -----------------------------------

		public override void OnRefresh()
		{
			TabControl.TabPages.Clear();

			// Fill the property grid.
			XmlNode UINode = XmlHelper.Find(Data, "ui");
			if ((UINode != null) && UINode.HasChildNodes) {
				TabControl.TabPages.Add(PropertiesTabPage);
				GenericUI.OnLoad(Controller, NodePath, UINode.OuterXml);
				GenericUI.OnRefresh();
			}

			// Create tabs for each script tag.
			foreach (XmlNode Script in XmlHelper.ChildNodes(Data, "script")) {
				string TabName = "";
				foreach (XmlNode EventData in XmlHelper.ChildNodes(Script, "event")) {
					if (!string.IsNullOrEmpty(TabName)) {
						TabName = TabName + ",";
					}
					TabName = TabName + EventData.InnerText;
				}
				string Value = XmlHelper.Value(Script, "text");
				AddScriptTab(TabName, Value);
			}

		}

		private void AddScriptTab(string TabName, string Value)
		{
			TabPage page = new TabPage(TabName);

			//Add a menu to the page at the top.

			ToolStrip ToolStrip = new ToolStrip();
			ToolStrip.Parent = page;
			ToolStrip.ImageList = ImageList;
			ToolStrip.Dock = DockStyle.Top;
			
			ToolStripLabel SearchLabel = new ToolStripLabel("Search:");

			ToolStripTextBox SearchTextBox = new ToolStripTextBox("textbox");
			SearchTextBox.Width = 120;
			SearchTextBox.KeyDown += new KeyEventHandler(OnEnterKeyPressed);
			
			ToolStripButton SearchButton = new ToolStripButton();
			SearchButton.ImageIndex = 0;
			SearchButton.Click += new EventHandler(OnSearchButtonClicked);
            ToolStrip.Items.Add(SearchLabel);
            ToolStrip.Items.Add(SearchTextBox);
            ToolStrip.Items.Add(SearchButton);
			
            TextBox ScriptBox = new TextBox();
            ScriptBox.Font = new Font(FontFamily.GenericMonospace, 9);
            ScriptBox.WordWrap = false;
            ScriptBox.AcceptsReturn = true;
            ScriptBox.AcceptsTab = true;
            ScriptBox.Multiline = true;
            ScriptBox.ScrollBars = ScrollBars.Vertical;
            ScriptBox.Lines = Value.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            
			page.Controls.Add(ScriptBox);
			ScriptBox.Dock = DockStyle.Fill;
			ScriptBox.BringToFront();
			//int[] TabStops = { 3 };
			//ScriptBox.Lines.TabStops = TabStops;
			//ScriptBox.Lines.UseSpaces = true;
			TabControl.TabPages.Add(page);
		}

		public override void OnSave()
		{
			// --------------------------------------
			// Save the script box if it has changd.
			// --------------------------------------
			string Contents = "";
			if (TabControl.TabPages.Count > 0 && TabControl.TabPages[0].Text == "Properties") {
				GenericUI.OnSave();
				Contents = GenericUI.GetData();
			}
			Data.RemoveAll();

			if (!string.IsNullOrEmpty(Contents)) {
				XmlDocument Doc = new XmlDocument();
				Doc.LoadXml(Contents);
				Data.AppendChild(Data.OwnerDocument.ImportNode(Doc.DocumentElement, true));
			}

			foreach (TabPage Page in TabControl.TabPages) {
				if (Page.Text != "Properties") {
					XmlNode Script = Data.AppendChild(Data.OwnerDocument.CreateElement("script"));
					TextBox ScriptBox = (TextBox)Page.Controls[0];
					XmlHelper.SetValue(Script, "text", ScriptBox.Text);

					string[] EventNames = Page.Text.Split(",".ToCharArray());
					List<string> Events = new List<string>();
					Events.AddRange(EventNames);
					XmlHelper.SetValues(Script, "event", Events);
				}
			}
		}

		private void OnAddMenuClick(System.Object sender, System.EventArgs e)
		{
			string EventNamesString = UIBits.InputDialog.InputBox("Enter event name(s) to run script on", "APSIM event names (comma separated)", "", false);
			if (!string.IsNullOrEmpty(EventNamesString)) {
				AddScriptTab(EventNamesString, "");
				TabControl.SelectedIndex = TabControl.TabCount - 1;
			}
		}

		private void OnDeleteMenuClick(System.Object sender, System.EventArgs e)
		{
			string CurrentTabName = TabControl.TabPages[TabControl.SelectedIndex].Text;
			if (MessageBox.Show("Are you sure you want to delete " + CurrentTabName + "?", "Confirmation required", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
				TabControl.TabPages.Remove(TabControl.SelectedTab);
			}
		}

		private void OnEditMenuClick(System.Object sender, System.EventArgs e)
		{
			string EventNamesString = UIBits.InputDialog.InputBox("Enter event name(s) to run script on", "APSIM event names (comma separated)", TabControl.SelectedTab.Text, false);
			if (EventNamesString != TabControl.SelectedTab.Text) {
				TabControl.SelectedTab.Text = EventNamesString;
			}
		}

		private void OnPropertiesMenuClick(System.Object sender, System.EventArgs e)
		{
			TabControl.TabPages.Insert(0, PropertiesTabPage);
			XmlNode UINode = XmlHelper.Find(Data, "ui");
			if ((UINode == null)) {
				UINode = Data.AppendChild(Data.OwnerDocument.CreateElement("ui"));
			}

			GenericUI.OnLoad(Controller, NodePath, UINode.OuterXml);
			GenericUI.OnRefresh();
		}

		private void OnPopupOpening(System.Object sender, System.ComponentModel.CancelEventArgs e)
		{
			DeleteMenuItem.Enabled = TabControl.TabPages.Count > 1;
			EditMenuItem.Enabled = TabControl.SelectedTab.Text != "Properties";
			PropertiesMenuItem.Enabled = TabControl.TabPages[0].Text != "Properties";
		}

		private void OnSearchButtonClicked(object sender, EventArgs e)
		{
			ToolStrip ts = (ToolStrip) TabControl.SelectedTab.Controls[1];
			ToolStripItem [] tt = ts.Items.Find("textbox",true);
			if (tt.Length > 0) {
			string s = tt[0].Text;
			TextBox ScriptBox = (TextBox)TabControl.SelectedTab.Controls[0];
			SearchString(ScriptBox, s);
            ScriptBox.Focus();
			}
		}
		private void OnEnterKeyPressed(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter) 
			{
			string s = ((ToolStripTextBox)sender).Text;
			TextBox ScriptBox = (TextBox)TabControl.SelectedTab.Controls[0];
			SearchString(ScriptBox, s);
			}
		}
		
		private void SearchString(TextBox ScriptBox, string text)
		{
			if (text.Length > 0) {
				ScriptBox.SelectionStart = ScriptBox.SelectionStart + 1;
				
				int indexToText = ScriptBox.Text.IndexOf( text, ScriptBox.SelectionStart);
				if (indexToText == -1) {
					ScriptBox.SelectionStart = 0;
					ScriptBox.SelectionLength = 0;
				} else {
				    ScriptBox.SelectionStart = indexToText;
					ScriptBox.SelectionLength = text.Length;
				}
			    ScriptBox.ScrollToCaret();
			}
		}
	}
}
