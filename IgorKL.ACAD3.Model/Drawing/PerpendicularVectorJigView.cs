﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;

namespace IgorKL.ACAD3.Model.Drawing {
    public class PerpendicularVectorJigView : DrawJig {
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
            : this(ucs) {
            _baseCurve = baseCurve;
        }

        protected PerpendicularVectorJigView(Matrix3d ucs)
            : base() {
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
        public void AddKeyword(string globalName, string localName, string displayName, bool visible, bool enabled) {
            _keywords.Add(globalName, localName, displayName, visible, enabled);
        }
        public void AddKeywords(KeywordCollection keywords) {
            foreach (Keyword key in keywords)
                _keywords.Add(key.GlobalName, key.LocalName, key.DisplayName, key.Visible, key.Enabled);
        }
        protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw) {
            lock (_safeObject) {
                if (_entityInMemory != null)
                    if (!_entityInMemory.IsDisposed)
                        _entityInMemory.Dispose();

                Line line = new Line(_jppr.Value, _baseCurve.GetClosestPointTo(_jppr.Value, false));
                if (line != null) {
                    _jigPoint = line.StartPoint;
                    _jigBasePoint = line.EndPoint;
                } else
                    return false;

                try {
                    _entityInMemory = new Line(_jigPoint, _jigBasePoint);
                    _entityInMemory.SetDatabaseDefaults();
                    return draw.Geometry.Draw(_entityInMemory);
                } catch (Exception ex) {
                    return false;
                }
            }
        }

        protected override SamplerStatus Sampler(JigPrompts prompts) {
            if (_jppo == null) {
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

            return SamplerStatus.OK;

        }

        public PromptStatus StartJig() {
            PromptStatus res = PromptStatus.Other;
            while ((res = Tools.GetAcadEditor().Drag(this).Status) != PromptStatus.Cancel) {
                if (res == PromptStatus.Keyword) {
                    switch (_jppr.StringResult) {
                        case "Exit": {
                            return PromptStatus.Cancel;
                        }
                        default: {
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
