using System.Collections.Generic;
using Packages.UIForia.Src.VectorGraphics;
using TMPro;
using UIForia.Extensions;
using UIForia.Text;
using UIForia.Util;
using UnityEngine;

namespace SVGX {

    public class SVGXRoot : MonoBehaviour {

        public new Camera camera;
        private Mesh mesh;
        private Mesh lineMesh;

        private GFX gfx;

        private ImmediateRenderContext ctx;

        [Range(0, 360f)] public float rotation;
        public float dot;

        public void Start() {
            ctx = new ImmediateRenderContext();
            gfx = new GFX(camera);
            gfx2 = new GFX2(camera);
            ctx2 = gfx2.CreateContext();
        }

        public Texture2D texture;

        public float strokeWidth = 5;
        public Color tintColor;
        [Range(0, 1)] public float strokeOpacity;
        [Range(0, 1)] public float fillOpacity;

        public ColorStop[] gradientStops;
        public float outlineWidth = 0.25f;
        public Color textOutline;
        public int textSize;
        public Color textColor;
        public float radiusTL;
        public float radiusTR;
        public float radiusBL;
        public float radiusBR;
        public float skewX = 0;
        public float skewY = 0;
        public Color fillColor = Color.yellow;

        public Vector2 thing1;
        public Vector2 thing2;
        public bool stroke = true;
        public bool fill = true;

        public StrokePlacement strokePlacement;

        public Color shadowColor = Color.black;
        public Color shadowTint = Color.clear;
        public float shadowIntensity = 0.4f;
        [Range(0, 1)] public float shadowSoftnessX = 0.16f;
        [Range(0, 1)] public float shadowSoftnessY = 0.16f;
        public Vector2 shadowOffset;
        public Rect shadowRect = new Rect(400, 340, 200, 200);

        private GFX2 gfx2;
        private VectorContext ctx2;

        public static ImmediateRenderContext CTX;
        public bool enableScissor = true;

        public Vector2 offset;
        
        public void Update() {
            camera.orthographic = true;
            camera.orthographicSize = Screen.height * 0.5f;

            ctx.Clear();
            
            if (enableScissor) {
                ctx.EnableScissorRect(new Rect(100, 100, 300, 300));
            }

            
            ctx.SetStrokeWidth(3f);
            ctx.SetStroke(new Color32(0, 181, 217, 255));
            ctx.RoundedRect(new Rect(offset.x + 100, offset.y + 100, 300, 100), 40, 40, 40, 40);
            ctx.Stroke();
            
      
//            ctx.SetFill(Color.red);
//            ctx.FillRect(400, 400, 100, 100);
//
//            ctx.SetFill(texture);
//            ctx.FillRect(50 + offset.x, 250 + offset.y, 400, 300);
//            
//            ctx.SetFill(Color.blue);
//            ctx.FillRect(150, 150, 100, 100);
            SVGXTextStyle style = new SVGXTextStyle();
            style.fontSize = 24;
            style.color = Color.black;
            TextInfo tf = TextUtil.CreateTextInfo(new TextUtil.TextSpan(TMP_FontAsset.defaultFontAsset, style, "Hello World"));
            List<LineInfo> lineInfos = TextUtil.Layout(tf, float.MaxValue);
            tf.lineInfos = ArrayPool<LineInfo>.CopyFromList(lineInfos);
            tf.lineCount = lineInfos.Count;
            TextUtil.ApplyLineAndWordOffsets(tf);
            
//            ctx.Text(100 + offset.x, 100 + offset.y, tf);
//            ctx.SetFill(Color.black);
//            ctx.Fill();
            
            ctx.BeginPath();
            ctx.MoveTo(offset.x, offset.y);
            ctx.LineTo(500, 500);
            ctx.SetStroke(Color.yellow);
            ctx.SetStrokeWidth(3f);
            ctx.Stroke();
            
            gfx.Render(ctx);
//            CTX = ctx;
//            ctx.Clear();
//            ctx2.Clear();
//            ctx.SetStroke(Color.red);
//            float sWidth = 1f;
//            ctx.SetStrokeWidth(sWidth);
//
//            Vector2 p0 = new Vector2(500, 100);
////            Vector2 p0 = new Vector2(450, 450);
//            Vector2 p1 = new Vector2(100, 300);
////            Vector2 p1 = new Vector2(600, 600);
//            p1 = p1.Rotate(p0, rotation);
//            Vector2 p2 = new Vector2(400, 500);
////            Vector2 p2 = new Vector2(700, 700);
//            Vector2 p3 = new Vector2(500,400);
//
//            ctx.MoveTo(p0);
//            ctx.LineTo(p1);
//            ctx.LineTo(p2);
//
////            ctx.Stroke();
//
//            ctx.BeginPath();
//            Vector2 toCurr = (p1 - p0).normalized;
//            Vector2 toNext = (p2 - p1).normalized;
//            Vector2 toCurrPerp = new Vector2(-toCurr.y, toCurr.x);
//            Vector2 toNextPerp = new Vector2(-toNext.y, toNext.x);
//
//            Vector2 miter = (toCurrPerp + toNextPerp).normalized;
////
////            ctx.MoveTo(p1);
////            ctx.LineTo(p1 + (miter * 100f));
////            ctx.MoveTo(p1);
////            ctx.LineTo(p1 - (miter * 100f));
////
////            ctx.Stroke();
////            
////            ctx.CircleFromCenter(v0.x, v0.y, 5f);
////            ctx.CircleFromCenter(v1.x, v1.y, 5f);
////            ctx.CircleFromCenter(v2.x, v2.y, 5f);
////            ctx.CircleFromCenter(v3.x, v3.y, 5f);
////            ctx.Stroke();
////            
////            ctx.BeginPath();
////            
//////            ctx.MoveTo(lineStart - (toEnd * 10f));
////            ctx.LineTo((p0 - (toNext * 100f)  + (toNextPerp * (sWidth))));
////            ctx.Stroke();
////            
////            ctx.SetStroke(Color.yellow);
////            ctx.BeginPath();
////            ctx.MoveTo(p0);
////            ctx.LineTo(p0 + (toNextPerp * (-sWidth)));
////            ctx.LineTo((p0 - (toNext * 100f)  + (toNextPerp * (-sWidth))));
////            ctx.Stroke();
//
//            ctx2.BeginPath();
//            ctx2.MoveTo(p0);
//            ctx2.LineTo(p1);
//            ctx2.LineTo(p2);
//            ctx2.LineTo(p3);
//            ctx2.Stroke();
//
//            gfx2.Render();
//            gfx.Render(ctx);
        }

    }

}