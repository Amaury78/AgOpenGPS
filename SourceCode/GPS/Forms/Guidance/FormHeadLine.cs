﻿using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AgOpenGPS
{
    public partial class FormHeadLine : Form
    {
        //access to the main GPS form and all its variables
        private readonly FormGPS mf = null;

        private Point fixPt;

        private bool isA = true;
        private int start = 99999, end = 99999;
        private int bndSelect = 0, mode;
        public List<vec3> sliceArr = new List<vec3>();
        public List<vec3> backupList = new List<vec3>();

        public vec3 pint = new vec3(0.0, 1.0, 0.0);

        public FormHeadLine(Form callingForm)
        {
            //get copy of the calling main form
            mf = callingForm as FormGPS;

            InitializeComponent();

            mf.CalculateMinMax();
        }

        private void FormHeadLine_Load(object sender, EventArgs e)
        {
            mf.hdl.idx = -1;
            label3.Text = mf.unitsFtM;

            lblToolWidth.Text = ((mf.tool.width - mf.tool.overlap) * mf.m2FtOrM).ToString("N1") + " " + mf.unitsFtM;

            start = 99999; end = 99999;
            isA = true;
            mf.hdl.desList?.Clear();
            sliceArr?.Clear();
            backupList?.Clear();

            btnSlice.Enabled = false;

            mf.bnd.bndList[0].hdLine?.Clear();

            if (mf.bnd.bndList[0].fenceLine.Count > 0)
            {
                for (int i = 0; i < mf.bnd.bndList[0].fenceLine.Count; i++)
                {
                    mf.bnd.bndList[0].hdLine.Add(new vec3(mf.bnd.bndList[0].fenceLine[i]));
                }
            }
        }

        private void FormHeadLine_FormClosing(object sender, FormClosingEventArgs e)
        {
            //hdl
            if (mf.hdl.idx == -1)
            {
                if (mf.isAutoSteerBtnOn) mf.btnAutoSteer.PerformClick();
                if (mf.yt.isYouTurnBtnOn) mf.btnAutoYouTurn.PerformClick();
            }

            if (sliceArr.Count > 0)
            {
                mf.hdl.idx = 0;
            }
            else mf.hdl.idx = -1;
        }

        private void oglSelf_MouseDown(object sender, MouseEventArgs e)
        {
            if (nudSetDistance.Value == 0)
            {
                mf.TimedMessageBox(3000, "Distance Error", "Distance Set to 0, Nothing to Move");
                return;
            }
            sliceArr?.Clear();

            Point ptt = oglSelf.PointToClient(Cursor.Position);

            //Convert to Origin in the center of window, 800 pixels
            fixPt.X = ptt.X - 350;
            fixPt.Y = (700 - ptt.Y - 350);
            vec3 plotPt = new vec3
            {
                //convert screen coordinates to field coordinates
                easting = fixPt.X * mf.maxFieldDistance / 632.0,
                northing = fixPt.Y * mf.maxFieldDistance / 632.0,
                heading = 0
            };

            plotPt.easting += mf.fieldCenterX;
            plotPt.northing += mf.fieldCenterY;

            pint.easting = plotPt.easting;
            pint.northing = plotPt.northing;

            if (isA)
            {
                double minDistA = double.MaxValue;
                start = 99999; end = 99999;

                for (int j = 0; j < mf.bnd.bndList.Count; j++)
                {
                    for (int i = 0; i < mf.bnd.bndList[j].fenceLine.Count; i++)
                    {
                        double dist = ((pint.easting - mf.bnd.bndList[j].fenceLine[i].easting) * (pint.easting - mf.bnd.bndList[j].fenceLine[i].easting))
                                        + ((pint.northing - mf.bnd.bndList[j].fenceLine[i].northing) * (pint.northing - mf.bnd.bndList[j].fenceLine[i].northing));
                        if (dist < minDistA)
                        {
                            minDistA = dist;
                            bndSelect = j;
                            start = i;
                        }
                    }
                }

                isA = false;
            }
            else
            {
                double minDistA = double.MaxValue;
                int j = bndSelect;

                for (int i = 0; i < mf.bnd.bndList[j].fenceLine.Count; i++)
                {
                    double dist = ((pint.easting - mf.bnd.bndList[j].fenceLine[i].easting) * (pint.easting - mf.bnd.bndList[j].fenceLine[i].easting))
                                    + ((pint.northing - mf.bnd.bndList[j].fenceLine[i].northing) * (pint.northing - mf.bnd.bndList[j].fenceLine[i].northing));
                    if (dist < minDistA)
                    {
                        minDistA = dist;
                        end = i;
                    }
                }

                isA = true;

                //build the lines
                if (rbtnCurve.Checked)
                {
                    bool isLoop = false;
                    int limit = end;

                    if ((Math.Abs(start - end)) > (mf.bnd.bndList[bndSelect].fenceLine.Count * 0.5))
                    {
                        if (start < end)
                        {
                            (start, end) = (end, start);
                        }

                        isLoop = true;
                        if (start < end)
                        {
                            limit = end;
                            end = 0;
                        }
                        else
                        {
                            limit = end;
                            end = mf.bnd.bndList[bndSelect].fenceLine.Count;
                        }
                    }
                    else
                    {
                        if (start > end)
                        {
                            (start, end) = (end, start);
                        }
                    }

                    sliceArr?.Clear();
                    vec3 pt3 = new vec3();

                    if (start < end)
                    {
                        for (int i = start; i <= end; i++)
                        {
                            //calculate the point inside the boundary
                            pt3 = mf.bnd.bndList[bndSelect].fenceLine[i];
                            sliceArr.Add(pt3);

                            if (isLoop && i == mf.bnd.bndList[bndSelect].fenceLine.Count - 1)
                            {
                                i = -1;
                                isLoop = false;
                                end = limit;
                            }
                        }
                    }
                    else
                    {
                        for (int i = start; i >= end; i--)
                        {
                            //calculate the point inside the boundary
                            pt3 = mf.bnd.bndList[bndSelect].fenceLine[i];
                            sliceArr.Add(pt3);

                            if (isLoop && i == 0)
                            {
                                i = mf.bnd.bndList[bndSelect].fenceLine.Count - 1;
                                isLoop = false;
                                end = limit;
                            }
                        }
                    }

                    int ptCnt = sliceArr.Count - 1;

                    if (ptCnt > 0)
                    {
                        //who knows which way it actually goes
                        mf.hdl.CalculateHeadings(ref sliceArr);

                        for (int i = 1; i < 75; i++)
                        {
                            vec3 pt = new vec3(sliceArr[ptCnt]);
                            pt.easting += (Math.Sin(pt.heading) * i);
                            pt.northing += (Math.Cos(pt.heading) * i);
                            sliceArr.Add(pt);
                        }

                        vec3 stat = new vec3(sliceArr[0]);

                        for (int i = 1; i < 75; i++)
                        {
                            vec3 pt = new vec3(stat);
                            pt.easting -= (Math.Sin(pt.heading) * i);
                            pt.northing -= (Math.Cos(pt.heading) * i);
                            sliceArr.Insert(0, pt);
                        }

                        mode = (int)TrackMode.Curve;
                    }
                    else
                    {
                        start = 99999; end = 99999;
                        return;
                    }

                    //update the arrays
                    start = 99999; end = 99999;

                    btnExit.Focus();
                }
                else if (rbtnLine.Checked)
                {
                    if ((Math.Abs(start - end)) > (mf.bnd.bndList[bndSelect].fenceLine.Count * 0.5))
                    {
                        if (start < end)
                        {
                            (start, end) = (end, start);
                        }
                    }
                    else
                    {
                        if (start > end)
                        {
                            (start, end) = (end, start);
                        }
                    }

                    vec3 ptA = new vec3(mf.bnd.bndList[bndSelect].fenceLine[start]);
                    vec3 ptB = new vec3(mf.bnd.bndList[bndSelect].fenceLine[end]);

                    //calculate the AB Heading
                    double abHead = Math.Atan2(
                        mf.bnd.bndList[bndSelect].fenceLine[end].easting - mf.bnd.bndList[bndSelect].fenceLine[start].easting,
                        mf.bnd.bndList[bndSelect].fenceLine[end].northing - mf.bnd.bndList[bndSelect].fenceLine[start].northing);
                    if (abHead < 0) abHead += glm.twoPI;

                    sliceArr?.Clear();

                    ptA.heading = abHead;
                    ptB.heading = abHead;

                    for (int i = 0; i <= (int)(glm.Distance(ptA, ptB)); i++)
                    {
                        vec3 ptC = new vec3(ptA);
                        ptC.easting = (Math.Sin(abHead) * i) + ptA.easting;
                        ptC.northing = (Math.Cos(abHead) * i) + ptA.northing;
                        ptC.heading = abHead;
                        sliceArr.Add(ptC);
                    }

                    int ptCnt = sliceArr.Count - 1;

                    for (int i = 1; i < 75; i++)
                    {
                        vec3 pt = new vec3(sliceArr[ptCnt]);
                        pt.easting += (Math.Sin(pt.heading) * i);
                        pt.northing += (Math.Cos(pt.heading) * i);
                        sliceArr.Add(pt);
                    }

                    vec3 stat = new vec3(sliceArr[0]);

                    for (int i = 1; i < 75; i++)
                    {
                        vec3 pt = new vec3(stat);
                        pt.easting -= (Math.Sin(pt.heading) * i);
                        pt.northing -= (Math.Cos(pt.heading) * i);
                        sliceArr.Insert(0, pt);
                    }

                    mode = (int)TrackMode.AB;

                    start = 99999; end = 99999;
                }

                //Move the line
                if (nudSetDistance.Value != 0)
                    SetLineDistance();

                btnSlice.Enabled = true;
            }
        }

        private void oglSelf_Paint(object sender, PaintEventArgs e)
        {
            oglSelf.MakeCurrent();

            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
            GL.LoadIdentity();                  // Reset The View

            //back the camera up
            GL.Translate(0, 0, -mf.maxFieldDistance);

            //translate to that spot in the world
            GL.Translate(-mf.fieldCenterX, -mf.fieldCenterY, 0);

            GL.Color3(1, 1, 1);

            //GL.Enable(EnableCap.Blend);

            //draw all the boundaries

            GL.LineWidth(1);

            for (int j = 0; j < mf.bnd.bndList.Count; j++)
            {
                if (j == bndSelect)
                    GL.Color3(0.25f, 0.5f, 0.20f);
                else
                    GL.Color3(0.50f, 0.25f, 0.10f);

                GL.Begin(PrimitiveType.LineStrip);
                for (int i = 0; i < mf.bnd.bndList[j].fenceLine.Count; i++)
                {
                    GL.Vertex3(mf.bnd.bndList[j].fenceLine[i].easting, mf.bnd.bndList[j].fenceLine[i].northing, 0);
                }
                GL.End();
            }

            //the vehicle
            GL.PointSize(8.0f);
            GL.Begin(PrimitiveType.Points);
            GL.Color3(0.95f, 0.90f, 0.0f);
            GL.Vertex3(mf.pivotAxlePos.easting, mf.pivotAxlePos.northing, 0.0);
            GL.End();

            //draw the line building graphics
            if (start != 99999 || end != 99999) DrawABTouchLine();

            //draw the actual built lines
            //if (start == 99999 && end == 99999)
            {
                DrawBuiltLines();
            }

            GL.Disable(EnableCap.Blend);

            GL.Flush();
            oglSelf.SwapBuffers();
        }

        private void DrawBuiltLines()
        {
            GL.LineWidth(4);
            GL.Color3(0.93f, 0.599f, 0.50f);
            GL.Begin(PrimitiveType.LineStrip);

            for (int i = 0; i < mf.bnd.bndList[0].hdLine.Count; i++)
            {
                GL.Vertex3(mf.bnd.bndList[0].hdLine[i].easting, mf.bnd.bndList[0].hdLine[i].northing, 0);
            }
            GL.End();

            if (sliceArr.Count > 0)
            {
                //GL.Enable(EnableCap.LineStipple);
                GL.LineStipple(1, 0x7070);
                GL.PointSize(2);

                if (mode == (int)TrackMode.AB)
                {
                    GL.Color3(0.973f, 0.19f, 0.10f);
                }
                else
                {
                    GL.Color3(0.3f, 0.99f, 0.20f);
                }

                GL.Begin(PrimitiveType.Points);
                foreach (vec3 item in sliceArr)
                {
                    GL.Vertex3(item.easting, item.northing, 0);
                }
                GL.End();

                int cnt = sliceArr.Count - 1;
                GL.PointSize(12);
                GL.Color3(1.0f, 0.6f, 0.3f);
                GL.Begin(PrimitiveType.Points);
                GL.Vertex3(sliceArr[0].easting, sliceArr[0].northing, 0);
                GL.Color3(0.3f, 0.3f, 0.99f);
                GL.Vertex3(sliceArr[cnt].easting, sliceArr[cnt].northing, 0);
                GL.End();
            }
        }

        private void DrawABTouchLine()
        {
            GL.Color3(0.65, 0.650, 0.0);
            GL.PointSize(16);
            GL.Begin(PrimitiveType.Points);

            GL.Color3(0, 0, 0);
            if (start != 99999) GL.Vertex3(mf.bnd.bndList[bndSelect].fenceLine[start].easting, mf.bnd.bndList[bndSelect].fenceLine[start].northing, 0);
            if (end != 99999) GL.Vertex3(mf.bnd.bndList[bndSelect].fenceLine[end].easting, mf.bnd.bndList[bndSelect].fenceLine[end].northing, 0);
            GL.End();

            GL.PointSize(10);
            GL.Begin(PrimitiveType.Points);

            GL.Color3(1.0f, 0.75f, 0.350f);
            if (start != 99999) GL.Vertex3(mf.bnd.bndList[bndSelect].fenceLine[start].easting, mf.bnd.bndList[bndSelect].fenceLine[start].northing, 0);

            GL.Color3(0.5f, 0.5f, 1.0f);
            if (end != 99999) GL.Vertex3(mf.bnd.bndList[bndSelect].fenceLine[end].easting, mf.bnd.bndList[bndSelect].fenceLine[end].northing, 0);
            GL.End();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            oglSelf.Refresh();
            if (sliceArr.Count == 0)
            {
                btnSlice.Enabled = false;
                btnALength.Enabled = false;
                btnBLength.Enabled = false;
            }
            else
            {
                btnSlice.Enabled = true;
                btnBLength.Enabled = true;
                btnALength.Enabled = true;
            }

            if (backupList.Count == 0) btnUndo.Enabled = false; else btnUndo.Enabled = true;
            if (nudSetDistance.Value == 0) btnBndLoop.Enabled = false; else btnBndLoop.Enabled = true;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            vec3[] hdArr;

            if (mf.bnd.bndList[0].hdLine.Count > 0)
            {
                hdArr = new vec3[mf.bnd.bndList[0].hdLine.Count];
                mf.bnd.bndList[0].hdLine.CopyTo(hdArr);
                mf.bnd.bndList[0].hdLine?.Clear();

                //does headland control sections
                mf.bnd.isSectionControlledByHeadland = cboxIsSectionControlled.Checked;
                Properties.Settings.Default.setHeadland_isSectionControlled = cboxIsSectionControlled.Checked;
                Properties.Settings.Default.Save();

                //middle points
                for (int i = 1; i < hdArr.Length; i++)
                {
                    hdArr[i - 1].heading = Math.Atan2(hdArr[i - 1].easting - hdArr[i].easting, hdArr[i - 1].northing - hdArr[i].northing);
                    if (hdArr[i].heading < 0) hdArr[i].heading += glm.twoPI;
                }

                double delta = 0;
                for (int i = 0; i < hdArr.Length; i++)
                {
                    if (i == 0)
                    {
                        mf.bnd.bndList[0].hdLine.Add(new vec3(hdArr[i].easting, hdArr[i].northing, hdArr[i].heading));
                        continue;
                    }
                    delta += (hdArr[i - 1].heading - hdArr[i].heading);

                    if (Math.Abs(delta) > 0.005)
                    {
                        vec3 pt = new vec3(hdArr[i].easting, hdArr[i].northing, hdArr[i].heading);

                        mf.bnd.bndList[0].hdLine.Add(pt);
                        delta = 0;
                    }
                }
                vec3 ptEnd = new vec3(hdArr[hdArr.Length - 1].easting, hdArr[hdArr.Length - 1].northing, hdArr[hdArr.Length - 1].heading);

                mf.bnd.bndList[0].hdLine.Add(ptEnd);
                delta = 0;
            }

            mf.FileSaveHeadland();
            Close();
        }

        private void btnALength_Click(object sender, EventArgs e)
        {
            if (sliceArr.Count > 0)
            {
                //and the beginning
                vec3 start = new vec3(sliceArr[0]);

                for (int i = 1; i < 10; i++)
                {
                    vec3 pt = new vec3(start);
                    pt.easting -= (Math.Sin(pt.heading) * i);
                    pt.northing -= (Math.Cos(pt.heading) * i);
                    sliceArr.Insert(0, pt);
                }
            }
        }

        private void btnBLength_Click(object sender, EventArgs e)
        {
            if (sliceArr.Count > 0)
            {
                int ptCnt = sliceArr.Count - 1;

                for (int i = 1; i < 10; i++)
                {
                    vec3 pt = new vec3(sliceArr[ptCnt]);
                    pt.easting += (Math.Sin(pt.heading) * i);
                    pt.northing += (Math.Cos(pt.heading) * i);
                    sliceArr.Add(pt);
                }
            }
        }

        private void oglSelf_Resize(object sender, EventArgs e)
        {
            oglSelf.MakeCurrent();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            //58 degrees view
            Matrix4 mat = Matrix4.CreatePerspectiveFieldOfView(1.01f, 1.0f, 1.0f, 20000);
            GL.LoadMatrix(ref mat);

            GL.MatrixMode(MatrixMode.Modelview);
        }

        private void SetLineDistance()
        {
            mf.hdl.desList?.Clear();

            if (sliceArr.Count < 1) return;

            double distAway = (double)nudSetDistance.Value * mf.ftOrMtoM;

            double distSqAway = (distAway * distAway) - 0.01;
            vec3 point;

            int refCount = sliceArr.Count;
            for (int i = 0; i < refCount; i++)
            {
                point = new vec3(
                sliceArr[i].easting - (Math.Sin(glm.PIBy2 + sliceArr[i].heading) * distAway),
                sliceArr[i].northing - (Math.Cos(glm.PIBy2 + sliceArr[i].heading) * distAway),
                sliceArr[i].heading);
                bool Add = true;

                for (int t = 0; t < refCount; t++)
                {
                    double dist = ((point.easting - sliceArr[t].easting) * (point.easting - sliceArr[t].easting))
                        + ((point.northing - sliceArr[t].northing) * (point.northing - sliceArr[t].northing));
                    if (dist < distSqAway)
                    {
                        Add = false;
                        break;
                    }
                }

                if (Add)
                {
                    if (mf.hdl.desList.Count > 0)
                    {
                        double dist = ((point.easting - mf.hdl.desList[mf.hdl.desList.Count - 1].easting) * (point.easting - mf.hdl.desList[mf.hdl.desList.Count - 1].easting))
                            + ((point.northing - mf.hdl.desList[mf.hdl.desList.Count - 1].northing) * (point.northing - mf.hdl.desList[mf.hdl.desList.Count - 1].northing));
                        if (dist > 1)
                            mf.hdl.desList.Add(point);
                    }
                    else mf.hdl.desList.Add(point);
                }
            }

            sliceArr.Clear();

            for (int i = 0; i < mf.hdl.desList.Count; i++)
            {
                sliceArr.Add(new vec3(mf.hdl.desList[i]));
            }

            mf.hdl.desList?.Clear();
        }

        private void nudSetDistance_Click(object sender, EventArgs e)
        {
            mf.KeypadToNUD((NumericUpDown)sender, this);
            btnExit.Focus();
        }

        // Returns 1 if the lines intersect, otherwis
        public double iE = 0, iN = 0;

        public List<int> crossings = new List<int>(1);

        private void btnBndLoop_Click(object sender, EventArgs e)
        {
            int ptCount = mf.bnd.bndList[0].fenceLine.Count;

            if (nudSetDistance.Value == 0)
            {
                mf.hdl.desList.Clear();

                mf.bnd.bndList[0].hdLine?.Clear();

                for (int i = 0; i < ptCount; i++)
                {
                    mf.bnd.bndList[0].hdLine.Add(new vec3(mf.bnd.bndList[0].fenceLine[i]));
                }
            }
            else
            {
                mf.hdl.desList?.Clear();

                //outside point
                vec3 pt3 = new vec3();

                double moveDist = (double)nudSetDistance.Value * mf.m2FtOrM;
                double distSq = (moveDist) * (moveDist) * 0.999;

                //make the boundary tram outer array
                for (int i = 0; i < ptCount; i++)
                {
                    //calculate the point inside the boundary
                    pt3.easting = mf.bnd.bndList[0].fenceLine[i].easting -
                        (Math.Sin(glm.PIBy2 + mf.bnd.bndList[0].fenceLine[i].heading) * (moveDist));

                    pt3.northing = mf.bnd.bndList[0].fenceLine[i].northing -
                        (Math.Cos(glm.PIBy2 + mf.bnd.bndList[0].fenceLine[i].heading) * (moveDist));

                    pt3.heading = mf.bnd.bndList[0].fenceLine[i].heading;

                    bool Add = true;

                    for (int j = 0; j < ptCount; j++)
                    {
                        double check = glm.DistanceSquared(pt3.northing, pt3.easting,
                                            mf.bnd.bndList[0].fenceLine[j].northing, mf.bnd.bndList[0].fenceLine[j].easting);
                        if (check < distSq)
                        {
                            Add = false;
                            break;
                        }
                    }

                    if (Add)
                    {
                        if (mf.hdl.desList.Count > 0)
                        {
                            double dist = ((pt3.easting - mf.hdl.desList[mf.hdl.desList.Count - 1].easting) * (pt3.easting - mf.hdl.desList[mf.hdl.desList.Count - 1].easting))
                                + ((pt3.northing - mf.hdl.desList[mf.hdl.desList.Count - 1].northing) * (pt3.northing - mf.hdl.desList[mf.hdl.desList.Count - 1].northing));
                            if (dist > 1)
                                mf.hdl.desList.Add(pt3);
                        }
                        else mf.hdl.desList.Add(pt3);
                    }
                }

                pt3 = new vec3(mf.hdl.desList[0]);
                mf.hdl.desList.Add(pt3);

                //btnCancelTouch.Enabled = false;

                int cnt = mf.hdl.desList.Count;
                if (cnt > 3)
                {
                    pt3 = new vec3(mf.hdl.desList[0]);
                    mf.hdl.desList.Add(pt3);

                    //make sure distance isn't too big between points on Turn
                    for (int i = 0; i < cnt - 1; i++)
                    {
                        int j = i + 1;
                        //if (j == cnt) j = 0;
                        double distance = glm.Distance(mf.hdl.desList[i], mf.hdl.desList[j]);
                        if (distance > 1.2)
                        {
                            vec3 pointB = new vec3((mf.hdl.desList[i].easting + mf.hdl.desList[j].easting) / 2.0,
                                (mf.hdl.desList[i].northing + mf.hdl.desList[j].northing) / 2.0,
                                mf.hdl.desList[i].heading);

                            mf.hdl.desList.Insert(j, pointB);
                            cnt = mf.hdl.desList.Count;
                            i = -1;
                        }
                    }
                    //who knows which way it actually goes
                    mf.hdl.CalculateHeadings(ref mf.hdl.desList);

                    mf.bnd.bndList[0].hdLine.Clear();

                    //write out the Curve Points
                    foreach (vec3 item in mf.hdl.desList)
                    {
                        mf.bnd.bndList[0].hdLine.Add(item);
                    }
                }
            }

            mf.FileSaveHeadland();
        }

        private void btnSlice_Click(object sender, EventArgs e)
        {
            int startBnd = 0, endBnd = 0, startLine = 0, endLine = 0;
            int isStart = 0;

            if (sliceArr.Count == 0) return;

            //save a backup
            backupList?.Clear();
            foreach (var item in mf.bnd.bndList[0].hdLine)
            {
                backupList.Add(item);
            }

            for (int i = 0; i < sliceArr.Count - 2; i++)
            {
                for (int k = 0; k < mf.bnd.bndList[0].hdLine.Count - 2; k++)
                {
                    int res = GetLineIntersection(
                    sliceArr[i].easting,
                    sliceArr[i].northing,
                    sliceArr[i + 1].easting,
                    sliceArr[i + 1].northing,

                    mf.bnd.bndList[0].hdLine[k].easting,
                    mf.bnd.bndList[0].hdLine[k].northing,
                    mf.bnd.bndList[0].hdLine[k + 1].easting,
                    mf.bnd.bndList[0].hdLine[k + 1].northing,
                    ref iE, ref iN);
                    if (res == 1)
                    {
                        if (isStart == 0)
                        {
                            startBnd = k + 1;
                            startLine = i + 1;
                        }
                        else
                        {
                            endBnd = k + 1;
                            endLine = i;
                        }
                        isStart++;
                    }
                }
            }

            if (isStart < 2)
            {
                mf.TimedMessageBox(2000, "Error", "Crossings not Found");
                return;
            }

            //overlaps start finish
            if ((Math.Abs(startBnd - endBnd)) > (mf.bnd.bndList[bndSelect].fenceLine.Count * 0.5))
            {
                if (startBnd < endBnd)
                {
                    (startBnd, endBnd) = (endBnd, startBnd);
                }

                mf.hdl.desList?.Clear();

                //first bnd segment
                for (int i = endBnd; i < startBnd; i++)
                {
                    mf.hdl.desList.Add(mf.bnd.bndList[0].hdLine[i]);
                }

                for (int i = startLine; i < endLine; i++)
                {
                    mf.hdl.desList.Add(sliceArr[i]);
                }

                //build headline from desList
                mf.bnd.bndList[0].hdLine.Clear();

                foreach (var item in mf.hdl.desList)
                {
                    mf.bnd.bndList[0].hdLine.Add(item);
                }
            }
            // completely in between start finish
            else
            {
                if (startBnd > endBnd)
                {
                    (startBnd, endBnd) = (endBnd, startBnd);
                }

                mf.hdl.desList?.Clear();

                //first bnd segment
                for (int i = 0; i < startBnd; i++)
                {
                    mf.hdl.desList.Add(mf.bnd.bndList[0].hdLine[i]);
                }

                //line segment
                for (int i = startLine; i < endLine; i++)
                {
                    mf.hdl.desList.Add(sliceArr[i]);
                }

                //final bnd segment
                for (int i = endBnd; i < mf.bnd.bndList[0].hdLine.Count; i++)
                {
                    mf.hdl.desList.Add(mf.bnd.bndList[0].hdLine[i]);
                }

                //build headline from desList
                mf.bnd.bndList[0].hdLine.Clear();

                foreach (var item in mf.hdl.desList)
                {
                    mf.bnd.bndList[0].hdLine.Add(item);
                }
            }

            mf.hdl.desList?.Clear();
            sliceArr?.Clear();
        }

        private void btnDeletePoints_Click(object sender, EventArgs e)
        {
            start = 99999; end = 99999;
            isA = true;
            mf.hdl.desList?.Clear();
            sliceArr?.Clear();
            backupList?.Clear();
            mf.bnd.bndList[0].hdLine?.Clear();

            int ptCount = mf.bnd.bndList[0].fenceLine.Count;

            for (int i = 0; i < ptCount; i++)
            {
                mf.bnd.bndList[0].hdLine.Add(new vec3(mf.bnd.bndList[0].fenceLine[i]));
            }
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            mf.bnd.bndList[0].hdLine?.Clear();
            foreach (var item in backupList)
            {
                mf.bnd.bndList[0].hdLine.Add(item);
            }
            backupList?.Clear();
        }

        private void cboxToolWidths_SelectedIndexChanged(object sender, EventArgs e)
        {
            nudSetDistance.Value = (decimal)(Math.Round((mf.tool.width - mf.tool.overlap) * cboxToolWidths.SelectedIndex, 1));
        }

        private void btnHeadlandOff_Click(object sender, EventArgs e)
        {
            mf.bnd.bndList[0].hdLine?.Clear();
            mf.FileSaveHeadland();
            Close();
        }

        public int GetLineIntersection(double p0x, double p0y, double p1x, double p1y,
        double p2x, double p2y, double p3x, double p3y, ref double iEast, ref double iNorth)
        {
            double s1x, s1y, s2x, s2y;
            s1x = p1x - p0x;
            s1y = p1y - p0y;

            s2x = p3x - p2x;
            s2y = p3y - p2y;

            double s, t;
            s = (-s1y * (p0x - p2x) + s1x * (p0y - p2y)) / (-s2x * s1y + s1x * s2y);

            if (s >= 0 && s <= 1)
            {
                //check oher side
                t = (s2x * (p0y - p2y) - s2y * (p0x - p2x)) / (-s2x * s1y + s1x * s2y);
                if (t >= 0 && t <= 1)
                {
                    // Collision detected
                    iEast = p0x + (t * s1x);
                    iNorth = p0y + (t * s1y);
                    return 1;
                }
            }

            return 0; // No collision
        }

        private void oglSelf_Load(object sender, EventArgs e)
        {
            oglSelf.MakeCurrent();
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.ClearColor(0.22f, 0.22f, 0.22f, 1.0f);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        }
    }
}