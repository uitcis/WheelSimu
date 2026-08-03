using System;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace WheelSimu
{
    /// <summary>
    /// 自定义方向盘视图 — 赛车风格十字辐条（两横一竖）
    /// </summary>
    public class SteeringWheelView : View
    {
        private float _angle;
        private float _smoothAngle;

        // Paints
        private Paint _bgPaint;
        private Paint _rimPaint;
        private Paint _rimEdgePaint;
        private Paint _spokePaint;
        private Paint _spokeEdgePaint;
        private Paint _hubPaint;
        private Paint _hubInnerPaint;
        private Paint _gripPaint;
        private Paint _markerPaint;
        private Paint _angleTextPaint;
        private Paint _labelPaint;
        private Paint _arcPaint;

        private float _centerX, _centerY, _radius;
        private float _rimWidth;
        private Path _clipCircle = null!;

        public SteeringWheelView(Context context) : base(context) => Init();
        public SteeringWheelView(Context context, IAttributeSet attrs) : base(context, attrs) => Init();
        public SteeringWheelView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) => Init();

        public float Angle
        {
            get => _angle;
            set
            {
                _angle = value;
                _smoothAngle = value;
                Invalidate();
            }
        }

        private void Init()
        {
            // 背景 — 深色圆形底座
            _bgPaint = new Paint { AntiAlias = true };

            // 轮辋 (rim)
            _rimPaint = new Paint { AntiAlias = true };
            _rimPaint.SetStyle(Paint.Style.Stroke);

            _rimEdgePaint = new Paint { AntiAlias = true };
            _rimEdgePaint.SetStyle(Paint.Style.Stroke);
            _rimEdgePaint.StrokeWidth = 2.5f;
            _rimEdgePaint.Color = Color.Argb(255, 150, 150, 160);

            // 辐条 (spokes)
            _spokePaint = new Paint { AntiAlias = true };
            _spokePaint.SetStyle(Paint.Style.Fill);

            _spokeEdgePaint = new Paint { AntiAlias = true };
            _spokeEdgePaint.SetStyle(Paint.Style.Stroke);
            _spokeEdgePaint.Color = Color.Argb(255, 120, 120, 130);
            _spokeEdgePaint.StrokeWidth = 2f;

            // 中心 Hub
            _hubPaint = new Paint { AntiAlias = true };
            _hubPaint.SetStyle(Paint.Style.Fill);

            _hubInnerPaint = new Paint { AntiAlias = true };
            _hubInnerPaint.SetStyle(Paint.Style.Fill);

            // 握把凸起
            _gripPaint = new Paint { AntiAlias = true };
            _gripPaint.SetStyle(Paint.Style.Stroke);
            _gripPaint.Color = Color.Argb(255, 65, 65, 75);
            _gripPaint.StrokeWidth = 20f;
            _gripPaint.StrokeCap = Paint.Cap.Round;

            // 顶部正位标记
            _markerPaint = new Paint { AntiAlias = true };
            _markerPaint.SetStyle(Paint.Style.FillAndStroke);
            _markerPaint.StrokeWidth = 3f;
            _markerPaint.Color = Color.Argb(255, 255, 60, 50);

            // 角度文字
            _angleTextPaint = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(255, 230, 230, 240),
                TextSize = 32f,
                TextAlign = Paint.Align.Center,
                FakeBoldText = true,
            };

            _labelPaint = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(180, 160, 160, 170),
                TextSize = 22f,
                TextAlign = Paint.Align.Center,
            };

            // 角度弧线
            _arcPaint = new Paint { AntiAlias = true };
            _arcPaint.SetStyle(Paint.Style.Stroke);
            _arcPaint.StrokeWidth = 5f;
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            _centerX = w / 2f;
            _centerY = h / 2f;
            _radius = Math.Min(w, h) / 2f - 24f;
            _rimWidth = _radius * 0.13f;
            _rimPaint.StrokeWidth = _rimWidth;

            // 裁剪路径：限制转动部分不得超出轮辋外缘
            _clipCircle = new Path();
            _clipCircle.AddCircle(_centerX, _centerY, _radius + _rimWidth / 2f + 1f, Path.Direction.Cw);
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            DrawBackground(canvas);

            canvas.Save();

            // ============ 旋转画布以绘制方向盘主体 ============
            canvas.Rotate(_smoothAngle, _centerX, _centerY);

            // 裁剪：转动内容不超出轮辋外缘
            canvas.ClipPath(_clipCircle);

            // 外环 — 金属质感
            DrawRim(canvas);

            // 握把凸起 (上下左右四个位置)
            DrawGrip(canvas, 0);    // 右
            DrawGrip(canvas, 90);   // 下
            DrawGrip(canvas, 180);  // 左
            DrawGrip(canvas, 270);  // 上

            // 辐条 — 十字型：两横杠 + 一竖杠
            DrawCrossSpokes(canvas);

            // 中心轴承
            DrawHub(canvas);

            canvas.Restore();

            // ============ 不旋转的固定标记 ============
            DrawTopMarker(canvas);
            DrawTickMarks(canvas);
            DrawAngleArc(canvas);
            DrawAngleText(canvas);
        }

        // ================================================================
        //  背景
        // ================================================================
        private void DrawBackground(Canvas canvas)
        {
            float bgSize = _radius + 30f;
            var bgRect = new RectF(_centerX - bgSize, _centerY - bgSize,
                                   _centerX + bgSize, _centerY + bgSize);

            // 柔和的深色圆形背景
            var bgGrad = new RadialGradient(_centerX, _centerY, bgSize,
                new int[] { Color.Argb(255, 38, 38, 45), Color.Argb(255, 18, 18, 22) },
                new float[] { 0.6f, 1f },
                Shader.TileMode.Clamp);
            _bgPaint.SetShader(bgGrad);
            canvas.DrawRoundRect(bgRect, 30f, 30f, _bgPaint);
            _bgPaint.SetShader(null);

            // 外圈细边框
            var borderPaint = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(80, 200, 200, 210),
                StrokeWidth = 1.5f,
            };
            borderPaint.SetStyle(Paint.Style.Stroke);
            canvas.DrawRoundRect(new RectF(bgRect.Left + 1, bgRect.Top + 1, bgRect.Right - 1, bgRect.Bottom - 1),
                                 28f, 28f, borderPaint);
        }

        // ================================================================
        //  轮辋 (外环) — 金属渐变
        // ================================================================
        private void DrawRim(Canvas canvas)
        {
            // 金属银色渐变
            var rimGradient = new SweepGradient(_centerX, _centerY,
                new int[] {
                    Color.Argb(255, 170, 175, 185),
                    Color.Argb(255, 110, 115, 125),
                    Color.Argb(255, 190, 195, 205),
                    Color.Argb(255, 100, 105, 115),
                    Color.Argb(255, 170, 175, 185)
                },
                new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f });
            _rimPaint.SetShader(rimGradient);
            canvas.DrawCircle(_centerX, _centerY, _radius, _rimPaint);
            _rimPaint.SetShader(null);

            // 外边缘亮线
            canvas.DrawCircle(_centerX, _centerY, _radius + _rimWidth / 2f - 1f, _rimEdgePaint);
            // 内边缘暗线
            var innerEdge = new Paint(_rimEdgePaint);
            innerEdge.Color = Color.Argb(255, 80, 80, 88);
            canvas.DrawCircle(_centerX, _centerY, _radius - _rimWidth / 2f + 1f, innerEdge);

            // 环内部的细刻度环
            var tickRing = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(60, 180, 180, 190),
                StrokeWidth = 1f,
            };
            tickRing.SetStyle(Paint.Style.Stroke);
            canvas.DrawCircle(_centerX, _centerY, _radius - _rimWidth / 2f - 6f, tickRing);
        }

        // ================================================================
        //  握把凸起 (方向盘上 grip bumps)
        // ================================================================
        private void DrawGrip(Canvas canvas, float angleDeg)
        {
            float arcLen = 28f;
            var oval = new RectF(_centerX - _radius, _centerY - _radius,
                                 _centerX + _radius, _centerY + _radius);
            canvas.DrawArc(oval, angleDeg - arcLen / 2f, arcLen, false, _gripPaint);
        }

        // ================================================================
        //  十字辐条：两横杠 (← →) + 一竖杠 (↑)
        // ================================================================
        private void DrawCrossSpokes(Canvas canvas)
        {
            float hubRadius = _radius * 0.18f;
            float innerRimR = _radius - _rimWidth / 2f;
            float spokeHalfW = _radius * 0.06f;  // 辐条半宽

            // 横杠上下两条边，构成一个粗横杠
            // 辐条从 hub 延伸到轮辋内侧

            // --- 两横杠 (0° → 180°) ---
            // 上横杠边
            float topSpokeCenterY = _centerY - _radius * 0.08f;
            float botSpokeCenterY = _centerY + _radius * 0.08f;

            // 上横杠
            DrawSingleSpokeRect(canvas, _centerX - innerRimR, topSpokeCenterY - spokeHalfW,
                                _centerX + innerRimR, topSpokeCenterY + spokeHalfW);
            // 下横杠
            DrawSingleSpokeRect(canvas, _centerX - innerRimR, botSpokeCenterY - spokeHalfW,
                                _centerX + innerRimR, botSpokeCenterY + spokeHalfW);

            // --- 一竖杠 (270° → 90°, 即向上) ---
            float vertSpokeCenterX = _centerX;
            DrawSingleSpokeRect(canvas, vertSpokeCenterX - spokeHalfW, _centerY - innerRimR,
                                vertSpokeCenterX + spokeHalfW, _centerY - hubRadius);

            // 辐条与轮辋连接的倒角小三角 (美观)
            DrawSpokeGusset(canvas, -innerRimR, topSpokeCenterY - spokeHalfW, topSpokeCenterY + spokeHalfW, true);
            DrawSpokeGusset(canvas, innerRimR, topSpokeCenterY - spokeHalfW, topSpokeCenterY + spokeHalfW, true);
            DrawSpokeGusset(canvas, -innerRimR, botSpokeCenterY - spokeHalfW, botSpokeCenterY + spokeHalfW, true);
            DrawSpokeGusset(canvas, innerRimR, botSpokeCenterY - spokeHalfW, botSpokeCenterY + spokeHalfW, true);
            DrawSpokeGusset(canvas, vertSpokeCenterX, 0, 0, false);
        }

        private void DrawSingleSpokeRect(Canvas canvas, float left, float top, float right, float bottom)
        {
            // 渐变色填充
            var spokeGrad = new LinearGradient(left, top, right, bottom,
                new int[] { Color.Argb(255, 90, 90, 100), Color.Argb(255, 55, 55, 62) },
                new float[] { 0f, 1f },
                Shader.TileMode.Clamp);
            _spokePaint.SetShader(spokeGrad);
            canvas.DrawRect(new RectF(left, top, right, bottom), _spokePaint);
            _spokePaint.SetShader(null);

            // 边框
            var rect = new RectF(left, top, right, bottom);
            canvas.DrawRect(rect, _spokeEdgePaint);

            // 高光线
            var highlight = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(60, 220, 220, 230),
                StrokeWidth = 1.2f,
            };
            highlight.SetStyle(Paint.Style.Stroke);
            canvas.DrawLine(left, top + 1, right, top + 1, highlight);
        }

        // 辐条与轮辋连接的夹角填充
        private void DrawSpokeGusset(Canvas canvas, float xCenter, float yEdge1, float yEdge2, bool isHorizontal)
        {
            var path = new Path();
            float rimR = _radius - _rimWidth / 2f;
            float hubR = _radius * 0.18f;
            float gussetSize = _radius * 0.05f;

            if (isHorizontal)
            {
                // 横杠末端的倒角
                float sign = xCenter > 0 ? -1 : 1;
                path.MoveTo(xCenter, yEdge1);
                path.LineTo(xCenter + sign * gussetSize, yEdge1);
                path.LineTo(xCenter + sign * gussetSize, yEdge2);
                path.LineTo(xCenter, yEdge2);
            }
            else
            {
                // 竖杠顶端的倒角
                path.MoveTo(xCenter - (_radius * 0.06f), _centerY - rimR);
                path.LineTo(xCenter - (_radius * 0.06f), _centerY - rimR + gussetSize);
                path.LineTo(xCenter + (_radius * 0.06f), _centerY - rimR + gussetSize);
                path.LineTo(xCenter + (_radius * 0.06f), _centerY - rimR);
            }

            var gussetPaint = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(255, 70, 70, 78),
            };
            gussetPaint.SetStyle(Paint.Style.Fill);
            canvas.DrawPath(path, gussetPaint);
        }

        // ================================================================
        //  中心轴承 (Hub)
        // ================================================================
        private void DrawHub(Canvas canvas)
        {
            float hubR = _radius * 0.18f;

            // 外圈 — 金属齿轮感
            var hubOuterGrad = new RadialGradient(_centerX, _centerY, hubR,
                new int[] { Color.Argb(255, 200, 200, 215), Color.Argb(255, 130, 130, 145), Color.Argb(255, 80, 80, 90) },
                new float[] { 0f, 0.55f, 1f },
                Shader.TileMode.Clamp);
            _hubPaint.SetShader(hubOuterGrad);
            canvas.DrawCircle(_centerX, _centerY, hubR, _hubPaint);
            _hubPaint.SetShader(null);

            // 外圈边框
            var hubEdge = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(255, 180, 180, 195),
                StrokeWidth = 2f,
            };
            hubEdge.SetStyle(Paint.Style.Stroke);
            canvas.DrawCircle(_centerX, _centerY, hubR, hubEdge);

            // 内圈 — 深色凹陷
            float innerR = hubR * 0.55f;
            var hubInnerGrad = new RadialGradient(_centerX, _centerY, innerR,
                new int[] { Color.Argb(255, 40, 40, 46), Color.Argb(255, 20, 20, 25) },
                new float[] { 0f, 1f },
                Shader.TileMode.Clamp);
            _hubInnerPaint.SetShader(hubInnerGrad);
            canvas.DrawCircle(_centerX, _centerY, innerR, _hubInnerPaint);
            _hubInnerPaint.SetShader(null);

            // 内圈边框
            var innerEdge = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(255, 100, 100, 110),
                StrokeWidth = 1.5f,
            };
            innerEdge.SetStyle(Paint.Style.Stroke);
            canvas.DrawCircle(_centerX, _centerY, innerR, innerEdge);

            // 中心小圆点 — 亮色
            var dotPaint = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(255, 200, 200, 215),
            };
            dotPaint.SetStyle(Paint.Style.Fill);
            canvas.DrawCircle(_centerX, _centerY, hubR * 0.12f, dotPaint);

            // 十字螺丝纹
            float screwW = hubR * 0.35f;
            float screwThick = 3f;
            var screwPaint = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(200, 180, 180, 195),
                StrokeWidth = screwThick,
                StrokeCap = Paint.Cap.Round,
            };
            screwPaint.SetStyle(Paint.Style.Stroke);
            canvas.DrawLine(_centerX - screwW, _centerY, _centerX + screwW, _centerY, screwPaint);
            canvas.DrawLine(_centerX, _centerY - screwW, _centerX, _centerY + screwW, screwPaint);
        }

        // ================================================================
        //  顶部正位标记 (红色三角，不随方向盘旋转)
        // ================================================================
        private void DrawTopMarker(Canvas canvas)
        {
            float outerY = _centerY - _radius - _rimWidth / 2f - 3f;
            float innerY = outerY + 24f;
            float halfW = 10f;

            var path = new Path();
            path.MoveTo(_centerX, outerY);                    // 尖端朝上
            path.LineTo(_centerX - halfW, innerY);
            path.LineTo(_centerX + halfW, innerY);
            path.Close();

            canvas.DrawPath(path, _markerPaint);

            // 底部白色小标记
            var whiteDot = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(255, 255, 255, 255),
            };
            whiteDot.SetStyle(Paint.Style.Fill);
            canvas.DrawCircle(_centerX, _centerY - _radius - _rimWidth / 2f - 1f, 4f, whiteDot);
        }

        // ================================================================
        //  刻度标记 (±90°, 每30°)
        // ================================================================
        private void DrawTickMarks(Canvas canvas)
        {
            for (int a = -90; a <= 90; a += 30)
            {
                float rad = a * MathF.PI / 180f;
                float outerBase = _radius - _rimWidth / 2f - 8f;
                float innerBase = outerBase - 10f;

                float cos = MathF.Cos(rad);
                float sin = MathF.Sin(rad);

                float x1 = _centerX + innerBase * sin;
                float y1 = _centerY - innerBase * cos;
                float x2 = _centerX + outerBase * sin;
                float y2 = _centerY - outerBase * cos;

                var tickPaint = new Paint
                {
                    AntiAlias = true,
                    Color = a == 0 ? Color.Argb(200, 255, 80, 80) : Color.Argb(140, 180, 180, 190),
                    StrokeWidth = a % 60 == 0 ? 3.5f : 2f,
                    StrokeCap = Paint.Cap.Round,
                };
                tickPaint.SetStyle(Paint.Style.Stroke);
                canvas.DrawLine(x1, y1, x2, y2, tickPaint);
            }
        }

        // ================================================================
        //  角度弧线指示
        // ================================================================
        private void DrawAngleArc(Canvas canvas)
        {
            float margin = 12f;
            var oval = new RectF(_centerX - _radius + margin, _centerY - _radius + margin,
                                 _centerX + _radius - margin, _centerY + _radius - margin);

            // 背景弧 (灰色全范围 -90 ~ +90)
            var bgArc = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(30, 200, 200, 200),
                StrokeWidth = 5f,
                StrokeCap = Paint.Cap.Round,
            };
            bgArc.SetStyle(Paint.Style.Stroke);
            canvas.DrawArc(oval, -180, 180, false, bgArc);

            // 当前角度弧 (高亮)
            float sweep = -_angle;
            if (Math.Abs(sweep) > 0.5f)
            {
                var activeArc = new Paint
                {
                    AntiAlias = true,
                    Color = Color.Argb(160, 0, 200, 255),
                    StrokeWidth = 5f,
                    StrokeCap = Paint.Cap.Round,
                };
                activeArc.SetStyle(Paint.Style.Stroke);
                canvas.DrawArc(oval, -90, sweep, false, activeArc);
            }
        }

        // ================================================================
        //  角度数值显示
        // ================================================================
        private void DrawAngleText(Canvas canvas)
        {
            float textY = _centerY - _radius - 55f;
            canvas.DrawText($"{_angle:F1}°", _centerX, textY, _angleTextPaint);
            canvas.DrawText("STEERING", _centerX, textY + 28f, _labelPaint);
        }
    }
}
