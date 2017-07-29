using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing
{
   /* public class DrawElevationLeader:CustomObjects.EntityDrawer
    {
        public override void Calculate()
        {
            throw new NotImplementedException();
        }

        protected override Autodesk.AutoCAD.EditorInput.SamplerStatus Sampler(Autodesk.AutoCAD.EditorInput.JigPrompts prompts)
        {
            throw new NotImplementedException();
        }

        public class ElevationLeader:EntityJig
        {
            MLeader _mleader;
            List<Point3d> _points;
            Point3d _jigPoint;
            int _leaderLineIndex;
            int _leaderIndex;
            MLeader _leader;

            public ElevationLeader()
                :base(new MLeader())
            {
                _leader = (MLeader)Entity;

                _leaderLineIndex = -1;
                _leaderIndex = 
            }

            protected override bool Update()
            {
                try
                {
                    if (_points.Count == 0)
                    {

                    }
                }
                catch (System.Exception ex)
                {
                    Tools.GetAcadEditor().WriteMessage("\n{0}", ex.Message);
                }
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                JigPromptPointOptions opts =new JigPromptPointOptions();
                opts.UserInputControls = UserInputControls.Accept3dCoordinates;

                if (_points.Count == 0)
                {
                    opts.UserInputControls |= UserInputControls.NullResponseAccepted;
                    opts.Message = "\nУкажите проект: ";
                    opts.UseBasePoint = false;
                }

                else if (_points.Count == 1)
                {
                    opts.Message = "\nУкажите факт: ";
                    opts.BasePoint = _points.Last();
                    opts.UseBasePoint = true;
                }

                else if (_points.Count == 2)
                {
                    opts.UserInputControls |= UserInputControls.NullResponseAccepted;
                    opts.Message = "\nУкажите положение выноски: ";
                    opts.BasePoint = _points.Last();
                    opts.UseBasePoint = true;
                    opts.Keywords.Add("Exit", "ВЫХод", "ВЫХод", true, true);
                    opts.AppendKeywordsToMessage = true;
                }
                else // Should never happen
                    return SamplerStatus.Cancel;

                PromptPointResult res = prompts.AcquirePoint(opts);

                if (res.Status == PromptStatus.Keyword)
                {
                    if (res.StringResult == "Exit")
                    {
                        return SamplerStatus.Cancel;
                    }
                }

                if (_jigPoint == res.Value)
                {
                    return SamplerStatus.NoChange;
                }
                else if (res.Status == PromptStatus.OK)
                {
                    _jigPoint = res.Value;
                    return SamplerStatus.OK;
                }
                
                return SamplerStatus.Cancel;
            }

            public void AddVertex()
            {
                MLeader ml = Entity as MLeader;

                if (_points.Count == 0)
                {
                    int lineIndex = ml.AddLeaderLine(0);
                    ml.AddFirstVertex(lineIndex, _jigPoint);
                    ml.AddLastVertex(lineIndex, new Point3d(0,0,0));
                }

                ml.TextAttachmentType =
                            TextAttachmentType.AttachmentMiddle;
                _points.Add(_jigPoint);
            }
        }
    }*/
}
