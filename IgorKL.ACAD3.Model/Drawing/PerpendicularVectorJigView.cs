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
    public class PerpendicularVectorJigView : DrawJig
    {
        Point3d _jigBasePoint;
        Point3d _jigPoint;
        Curve _baseCurve;
        protected Entity _entityInMemory;
        Matrix3d _ucs;
        KeywordCollection _keywords;
        JigPromptPointOptions _jppo;
        PromptPointResult _jppr;
        Func<PromptPointResult, PromptStatus> _promptKeywordAction;
        object _safeObject;

        public PerpendicularVectorJigView(Curve baseCurve, Matrix3d ucs)
            :this(ucs)
        {
            //_baseCurve = (Curve)baseCurve.GetTransformedCopy(ucs);
            _baseCurve = baseCurve;
        }

        protected PerpendicularVectorJigView(Matrix3d ucs)
            :base()
        {
            _ucs = ucs;
            _jigPoint = Point3d.Origin;
            _jigBasePoint = Point3d.Origin;
            
            _keywords = new KeywordCollection();
            _keywords.Add("Exit", "ВЫХод", "ВЫХод", true, true);
            _entityInMemory = null;
            _safeObject = new object();
        }

        /// <summary>
        /// Удаленная точка, конечная точка нормальной линии
        /// </summary>
        public Point3d DestinationPoint { get { return _jigPoint; } }

        /// <summary>
        /// Точка на линии (определяет начало нормальной линии до определяемой точки)
        /// </summary>
        public Point3d NormalPoint { get { return _jigBasePoint; } }
        public Matrix3d Ucs { get { return _ucs; } }
        public Curve BaseCurve { get { return _baseCurve; } protected set { _baseCurve = value; } }
        public Func<PromptPointResult, PromptStatus> PromptKeywordAction { get { return _promptKeywordAction; } set { _promptKeywordAction = value; } }
        public void AddKeyword(string globalName, string localName, string displayName, bool visible, bool enabled)
        {
            _keywords.Add(globalName, localName, displayName, visible, enabled);
        }
        public void AddKeywords(KeywordCollection keywords)
        {
            foreach (Keyword key in keywords)
                _keywords.Add(key.GlobalName, key.LocalName, key.DisplayName, key.Visible, key.Enabled);
        }
        protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw)
        {
            lock (_safeObject)
            {
                if (_entityInMemory != null)
                    if (!_entityInMemory.IsDisposed)
                        _entityInMemory.Dispose();
                /*if (_jigPoint.IsEqualTo(_jigBasePoint))
                    return false;*/

                //Polyline pline = _baseCurve.ConvertToPolyline();

                //Line line = pline.GetOrthoNormalLine(_jppr.Value, null, false);
                Line line = new Line(_jppr.Value, _baseCurve.GetClosestPointTo(_jppr.Value, false));
                if (line != null)
                {
                    _jigPoint = line.StartPoint;
                    _jigBasePoint = line.EndPoint;
                }
                else
                    //throw new ArgumentNullException();
                    return false;

                /*_jigBasePoint = _jppr.Value;
                _jigPoint = _baseCurve.GetClosestPointTo(_jigBasePoint, false);*/

                try
                {
                    _entityInMemory = new Line(_jigPoint, _jigBasePoint);
                    _entityInMemory.SetDatabaseDefaults();
                    return draw.Geometry.Draw(_entityInMemory);
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (_jppo == null)
            {
                _jppo = new JigPromptPointOptions("\nУкажите точку: ");
                _jppo.UseBasePoint = true;
                _jppo.UserInputControls |= UserInputControls.Accept3dCoordinates;
                foreach (Keyword key in _keywords)
                    _jppo.Keywords.Add(key.GlobalName, key.LocalName, key.DisplayName, key.Visible, key.Enabled);
            }

            
            if (_jigPoint.IsEqualTo(_jigBasePoint))
                _jppo.BasePoint = _baseCurve.StartPoint;
            else
                _jppo.BasePoint = _jigBasePoint;

            _jppr = prompts.AcquirePoint(_jppo);
            if (_jppr.Status != PromptStatus.OK)
                return SamplerStatus.Cancel;
            if (_jppr.Value.IsEqualTo(_jigPoint))
                return SamplerStatus.NoChange;

            /*
            if (_baseCurve is Polyline)
            {
                Polyline pline = (Polyline)_baseCurve;
                Line line = pline.GetOrthoNormalLine(_jppr.Value);
                if (line != null)
                {
                    _jigBasePoint = line.StartPoint;
                    _jigPoint = line.EndPoint;
                    return SamplerStatus.OK;
                }
                else
                    return SamplerStatus.NoChange;
            }
            else if (_baseCurve is Arc)
            {
                Arc arc = (Arc)_baseCurve;
                Line normal = arc.GetOrthoNormalLine(_jppr.Value, false);
                if (normal != null)
                {
                    _jigBasePoint = normal.StartPoint;
                    _jigPoint = normal.EndPoint;
                    return SamplerStatus.OK;
                }
                else
                    return SamplerStatus.NoChange;
            }
            else if (_baseCurve is Line)
            {
                Line line = (Line)_baseCurve;
                Line normal = line.GetOrthoNormalLine(_jppr.Value, null, false);
                if (normal != null)
                {
                    _jigBasePoint = normal.StartPoint;
                    _jigPoint = normal.EndPoint;
                    return SamplerStatus.OK;
                }
                else
                    return SamplerStatus.NoChange;
            }*/


            /*Polyline pline = _baseCurve.ConvertToPolyline();
            Line line = pline.GetOrthoNormalLine(_jppr.Value);
            if (line != null)
            {
                _jigBasePoint = line.StartPoint;
                _jigPoint = line.EndPoint;
                return SamplerStatus.OK;
            }
            else
                return SamplerStatus.NoChange;*/

            return SamplerStatus.OK;

        }

        public PromptStatus StartJig()
        {
            PromptStatus res = PromptStatus.Other;
            while ((res = Tools.GetAcadEditor().Drag(this).Status) != PromptStatus.Cancel)
            {
                if (res == PromptStatus.Keyword)
                {
                    switch (_jppr.StringResult)
                    {
                        case "Exit":
                            {
                                return PromptStatus.Cancel;
                            }
                        default:
                            {
                                if (_promptKeywordAction(_jppr) != PromptStatus.OK)
                                    return PromptStatus.Cancel;
                                break;
                            }
                    }
                }
                if (res == PromptStatus.OK)
                    return res;

            }
            return res;
        }
    }
}
