﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESIL.DBUtility;
using BenMAP.Grid;

namespace BenMAP
{
    public partial class PollutantMulti : FormBase
    {

        public struct PollInfo
        {
            public int groupID;
            public string groupName;
            public int pollID;
            public string pollName;
            public int setupID;
            public int obsID;
        };

        public PollInfo setPollInfo(int pgID, string pgName, int pID, string pName, int sID, int oID)
        {
            PollInfo setPoll = new PollInfo();
            setPoll.groupID = pgID;
            setPoll.groupName = pgName;
            setPoll.pollID = pID;
            setPoll.pollName = pName;
            setPoll.setupID = sID;
            setPoll.obsID = oID;
            return setPoll;
        }

        public PollutantMulti()
        {
            InitializeComponent();
        }

        private void PollutantMulti_Load(object sender, EventArgs e)
        {
            try
            {
                FireBirdHelperBase fb = new ESILFireBirdHelper();
                string commandText = string.Empty;

                commandText = string.Format("select PGName from PollutantGroups where setupid={0} order by PollutantGroupID asc", CommonClass.MainSetup.SetupID);
                DataSet ds = fb.ExecuteDataset(CommonClass.Connection, CommandType.Text, commandText);

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    pollTreeView.Nodes.Add(dr["pgname"].ToString(), dr["pgname"].ToString());
                }

                commandText = string.Format("select PollutantGroupPollutants.PollutantGroupID, PGName, Pollutants.PollutantID, PollutantName, Pollutants.SetupID, ObservationType from Pollutants inner join PollutantGroupPollutants on Pollutants.PollutantID=PollutantGroupPollutants.PollutantID inner join PollutantGroups on PollutantGroups.PollutantGroupID=PollutantGroupPollutants.PollutantGroupID and PollutantGroups.SetupID={0} order by PollutantGroupPollutants.PollutantGroupID asc", CommonClass.MainSetup.SetupID);
                ds = fb.ExecuteDataset(CommonClass.Connection, CommandType.Text, commandText);

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    TreeNode[] newNode = pollTreeView.Nodes.Find(dr["PGName"].ToString(), false);
                    if (newNode[0] == null | newNode == null) break;
                    newNode[0].Nodes.Add(dr["pollutantname"].ToString(), dr["pollutantname"].ToString());
                    newNode[0].Collapse();

                    newNode = newNode[0].Nodes.Find(dr["pollutantname"].ToString(), false);
                    newNode[0].Tag = setPollInfo(Convert.ToInt32(dr["pollutantgroupid"]), dr["pgname"].ToString(), Convert.ToInt32(dr["pollutantid"]), dr["pollutantname"].ToString(), Convert.ToInt32(dr["setupid"]), Convert.ToInt32(dr["observationtype"]));
                } 

                /* from Pollutant.cs to reference for saving groups to global list 
                lstPollutant.DataSource = ds.Tables[0];
                lstPollutant.DisplayMember = "PollutantName";
                if (CommonClass.LstPollutant == null || CommonClass.LstPollutant.Count == 0) { CommonClass.LstPollutant = new List<BenMAPPollutant>(); return; }
                int selectedCount = CommonClass.LstPollutant.Count;
                string str = string.Empty; 
                for (int i = 0; i < selectedCount; i++)
                {
                    str = CommonClass.LstPollutant[i].PollutantName;
                    lstSPollutant.Items.Add(str);
                }*/
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }

        }

        private void groupTreeView_ItemDrag(object sender, System.Windows.Forms.ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void selectedTree_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }
        private void selectedTree_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                TreeView send = (TreeView)sender;
                TreeNode NewNode = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
                if(NewNode.Nodes.Count == 0) NewNode = NewNode.Parent;

                if (!send.Nodes.ContainsKey(NewNode.Name))
                {
                    if(send.GetNodeCount(false) > 0) send.Nodes.RemoveAt(0);
                    send.Nodes.Add((TreeNode)NewNode.Clone());
                } 
                else { MessageBox.Show(string.Format("{0} has already been selected.", NewNode.Text)); }
            }
        }

        private void showDetails (PollInfo pInfo) 
        {
            FireBirdHelperBase fb = new ESILFireBirdHelper();
            string commandText = string.Empty;
            try
            {
                txtPollutantName.Text = pInfo.pollName; 
                int obserID = pInfo.obsID;
                switch (obserID)
                {
                    case 0:
                        txtObservationType.Text = "Hourly";
                        tclFixed.Enabled = true;
                        if (cbShowDetails.Checked)
                        {
                            detailGroup.Height = 258;
                            this.Height = 652; 
                        }
                        else
                        {
                            this.Height = 386;
                        }
                        break;
                    case 1:
                        txtObservationType.Text = "Daily";
                        if (cbShowDetails.Checked)
                        {
                            detailGroup.Height = 85;
                            this.Height = 481; 
                        }
                        else
                        {
                            this.Height = 386;
                        }
                        break;
                }
                commandText = string.Format("select MetricName,MetricID,HourlyMetricGeneration from metrics where pollutantid={0}", pInfo.pollID);
                DataSet ds = fb.ExecuteDataset(CommonClass.Connection, new CommandType(), commandText);
                DataTable dtMetric = ds.Tables[0].Clone();
                dtMetric = ds.Tables[0].Copy();
                cmbMetric.DataSource = dtMetric;
                cmbMetric.DisplayMember = "MetricName";
                if (dtMetric.Rows.Count != 0)
                { cmbMetric.SelectedIndex = 0; }
                DataRowView drvMetric = cmbMetric.SelectedItem as DataRowView;
                if (drvMetric == null) { return; }
                commandText = string.Format("select SeasonalMetricName,SeasonalMetricID from SeasonalMetrics where MetricID={0}", drvMetric["metricID"]);
                ds = fb.ExecuteDataset(CommonClass.Connection, new CommandType(), commandText);
                cmbSeasonalMetric.DataSource = ds.Tables[0];
                cmbSeasonalMetric.DisplayMember = "SeasonalMetricName";

            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }
        }

        private void cbShowDetails_CheckedChanged(object sender, EventArgs e)
        {
            if (sender == null || pollTreeView.SelectedNode == null) { return; }
            if (pollTreeView.SelectedNode.Tag == null) { this.Height = 386; return; }
            PollInfo pInfo = (PollInfo)pollTreeView.SelectedNode.Tag;
            showDetails(pInfo);
        }

        public void loadMetric(DataRowView drvMetric)
        {
            FireBirdHelperBase fb = new ESILFireBirdHelper();
            string commandText = string.Empty;
            try
            {
                switch (Convert.ToInt32(drvMetric["HourlyMetricGeneration"]))
                {
                    case 1:
                        commandText = string.Format("select starthour,endhour,statistic from FixedWindowMetrics where metricid={0}", drvMetric["metricid"]);
                        DataSet ds = fb.ExecuteDataset(CommonClass.Connection, CommandType.Text, commandText);
                        if (ds.Tables[0] == null) { return; }
                        DataRow dr = ds.Tables[0].Rows[0];
                        txtStartHour.Text = dr["starthour"].ToString();
                        txtEndHour.Text = dr["endhour"].ToString();
                        cmbStatistic.SelectedIndex = Convert.ToInt32(dr["statistic"]) - 1;
                        cmbStatistic.Enabled = false;
                        tclFixed.Controls.Clear();
                        tclFixed.TabPages.Add(tabFixed);
                        tclFixed.Visible = true;
                        break;
                    case 2:
                        commandText = string.Format("select WindowSize,WindowStatistic,DailyStatistic from MovingWindowMetrics where metricid={0}", drvMetric["metricid"]);
                        ds = fb.ExecuteDataset(CommonClass.Connection, CommandType.Text, commandText);
                        if (ds.Tables[0].Rows.Count == 0) { return; }
                        dr = ds.Tables[0].Rows[0];
                        txtMovingWindowSize.Text = dr["WindowSize"].ToString();
                        cmbWStatistic.SelectedIndex = Convert.ToInt32(dr["WindowStatistic"]) - 1;
                        cmbWStatistic.Enabled = false;
                        cmbDaily.SelectedIndex = Convert.ToInt32(dr["DailyStatistic"]) - 1;
                        cmbDaily.Enabled = false;
                        tclFixed.Controls.Clear();
                        tclFixed.TabPages.Add(tabMoving);
                        tclFixed.Visible = true;
                        break;
                    case 0:
                        commandText = string.Format("select MetricFunction from CustomMetrics where MetricID={0}", drvMetric["metricid"]);
                        object obj = fb.ExecuteScalar(CommonClass.Connection, CommandType.Text, commandText);
                        if (obj != null)
                        {
                            txtFunction.Text = obj.ToString();
                            tclFixed.Controls.Clear();
                            tclFixed.TabPages.Add(tabCustom);
                            tclFixed.Visible = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }
        }

        private void selectedTree_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                TreeNode newNode = selectTreeView.SelectedNode;
                if (newNode != null)
                {
                    if (newNode.Nodes.Count == 0) newNode = newNode.Parent;
                    DialogResult result = MessageBox.Show(string.Format("Delete the selected pollutant(s) \'{0}\'? ", newNode.Text), "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result != DialogResult.Yes) { return; }

                    selectTreeView.Nodes.Remove(newNode);
                }
                else
                {
                    MessageBox.Show("There is no pollutant to delete.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectTreeView.Nodes.Count == 0)
                {
                    MessageBox.Show("Please select a pollutant.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (selectTreeView.Nodes.Count > 0) { selectTreeView.Nodes.Clear(); }
            this.DialogResult = DialogResult.Cancel;
        }

        private void cmbMetric_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                DataRowView drvMetric = cmbMetric.SelectedItem as DataRowView;
                if (drvMetric == null) { return; }
                loadMetric(drvMetric);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }
        }
   

        private void pollTreeView_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender == null) { return; }
                if (pollTreeView.SelectedNode.Tag == null) { this.Height = 386; return; }
                PollInfo pInfo = (PollInfo)pollTreeView.SelectedNode.Tag;
                showDetails(pInfo);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }
        }
    }
}
