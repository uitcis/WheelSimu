using System;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace WheelSimu
{
    /// <summary>
    /// 垂直踏板进度条：底部→顶部自动渐变填充，拖拽调整，实时显示百分比
    /// </summary>
    public class PedalGaugeView : View
    {
        private float _progress; // 0-100

        // 颜色
        private int _fillColor1, _fillColor2, _labelColor;
        private Android.Graphics.Color _accentColor;

        // Paints
        private Paint _bgPaint;
        private Paint _fillPaint;
        private Paint _shinePaint;
        private Paint _thumbPaint;
        private Paint _textPaint;
        private Paint _borderPaint;

        private RectF _drawRect;

        public event EventHandler<float> ProgressChanged;

        /// <summary>
        /// 互斥踏板：当本踏板值 &gt;0 时自动将对方归零 (油门↔刹车互斥)
        /// </summary>
        public PedalGaugeView LinkedPedal { get; set; }

        public PedalGaugeView(Context context) : base(context) => Init();
        public PedalGaugeView(Context context, IAttributeSet attrs) : base(context, attrs) => Init();
        public PedalGaugeView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) => Init();

        public float Progress
        {
            get => _progress;
            set
            {
                _progress = Math.Clamp(value, 0, 100);
                Invalidate();
            }
        }

        public void SetColors(int fillColor1, int fillColor2, int labelColor)
        {
            _fillColor1 = fillColor1;
            _fillColor2 = fillColor2;
            _labelColor = labelColor;
            Invalidate();
        }

        private void Init()
        {
            // 默认绿色
            _fillColor1 = Color.Argb(255, 56, 142, 60).ToArgb();
            _fillColor2 = Color.Argb(255, 27, 94, 32).ToArgb();
            _labelColor = Color.Argb(255, 76, 175, 80).ToArgb();
            _accentColor = Color.Argb(255, 76, 175, 80);

            // 背景
            _bgPaint = new Paint { AntiAlias = true };
            _bgPaint.SetStyle(Paint.Style.Fill);

            // 填充
            _fillPaint = new Paint { AntiAlias = true };
            _fillPaint.SetStyle(Paint.Style.Fill);

            // 亮线 (填充顶部高光)
            _shinePaint = new Paint
            {
                AntiAlias = true,
                StrokeWidth = 3f,
                StrokeCap = Paint.Cap.Round,
            };
            _shinePaint.SetStyle(Paint.Style.Stroke);

            // 滑块圆点
            _thumbPaint = new Paint { AntiAlias = true };
            _thumbPaint.SetStyle(Paint.Style.FillAndStroke);
            _thumbPaint.StrokeWidth = 2f;

            // 百分比文字
            _textPaint = new Paint
            {
                AntiAlias = true,
                TextSize = 30f,
                TextAlign = Paint.Align.Center,
                FakeBoldText = true,
            };

            // 边框
            _borderPaint = new Paint
            {
                AntiAlias = true,
                Color = Color.Argb(60, 200, 200, 210),
                StrokeWidth = 1.5f,
            };
            _borderPaint.SetStyle(Paint.Style.Stroke);

            _drawRect = new RectF();

            // 支持触摸
            Clickable = true;
            Focusable = true;
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            _drawRect.Set(PaddingLeft, PaddingTop, w - PaddingRight, h - PaddingBottom);
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            float w = _drawRect.Width();
            float h = _drawRect.Height();
            float left = _drawRect.Left;
            float top = _drawRect.Top;

            if (w <= 0 || h <= 0) return;

            float fillH = h * _progress / 100f;
            float fillTop = top + h - fillH;

            // --- 背景 ---
            var bgGrad = new LinearGradient(0, top, 0, top + h,
                new int[] { Color.Argb(255, 16, 20, 28).ToArgb(), Color.Argb(255, 8, 10, 14).ToArgb() },
                null, Shader.TileMode.Clamp);
            _bgPaint.SetShader(bgGrad);
            canvas.DrawRoundRect(_drawRect, 8f, 8f, _bgPaint);
            _bgPaint.SetShader(null);

            // --- 刻度线 ---
            var tickPaint = new Paint
            {
                AntiAlias = true,
                Color = Android.Graphics.Color.Argb(15, 220, 220, 230),
                StrokeWidth = 1f,
            };
            tickPaint.SetStyle(Paint.Style.Stroke);
            for (int i = 25; i < 100; i += 25)
            {
                float y = top + h - (h * i / 100f);
                canvas.DrawLine(left + 8, y, left + w - 8, y, tickPaint);
            }

            // --- 彩色填充 ---
            if (fillH > 0)
            {
                var fillRect = new RectF(left + 1, fillTop, left + w - 1, top + h - 1);
                var fillGrad = new LinearGradient(0, fillTop, 0, top + h,
                    new int[] { _fillColor1, _fillColor2 },
                    new float[] { 0f, 1f },
                    Shader.TileMode.Clamp);
                _fillPaint.SetShader(fillGrad);
                canvas.DrawRect(fillRect, _fillPaint);
                _fillPaint.SetShader(null);

                // 填充顶部亮线
                _shinePaint.Color = new Color(_accentColor);
                _shinePaint.Alpha = 200;
                canvas.DrawLine(left + 3, fillTop, left + w - 3, fillTop, _shinePaint);
            }

            // --- 滑块圆点 ---
            float thumbR = w * 0.28f;
            float thumbY = fillTop;
            _thumbPaint.Color = new Color(245, 245, 255);
            _thumbPaint.SetShadowLayer(6f, 0, 2f, Color.Argb(100, 0, 0, 0));
            canvas.DrawCircle(left + w / 2f, thumbY, thumbR, _thumbPaint);

            // 滑块内圈
            var thumbInner = new Paint { AntiAlias = true };
            thumbInner.SetStyle(Paint.Style.Fill);
            thumbInner.Color = new Color(_accentColor);
            canvas.DrawCircle(left + w / 2f, thumbY, thumbR * 0.5f, thumbInner);

            // --- 边框 ---
            canvas.DrawRoundRect(_drawRect, 8f, 8f, _borderPaint);

            // --- 百分比文字 ---
            _textPaint.Color = new Color(_labelColor);
            float textX = left + w / 2f;
            float textY = top + h / 2f + 10f;

            // 文字背景阴影
            _textPaint.SetShadowLayer(4f, 0, 1f, Color.Argb(180, 0, 0, 0));
            canvas.DrawText($"{_progress:F0}%", textX, textY, _textPaint);
            _textPaint.SetShadowLayer(0, 0, 0, Color.Transparent);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (!Enabled) return base.OnTouchEvent(e);

            float h = _drawRect.Height();
            float top = _drawRect.Top;

            var action = e.ActionMasked;
            if (action == MotionEventActions.Down || action == MotionEventActions.Move)
            {
                float y = e.GetY();
                float newProgress = 100f - ((y - top) / h * 100f);
                newProgress = Math.Clamp(newProgress, 0, 100);

                if (Math.Abs(newProgress - _progress) > 0.5f || action == MotionEventActions.Down)
                {
                    _progress = newProgress;

                    // 互斥：本踏板 >0 时，将关联踏板归零
                    if (_progress > 0 && LinkedPedal != null && LinkedPedal.Progress > 0)
                    {
                        LinkedPedal.Progress = 0;
                    }

                    ProgressChanged?.Invoke(this, _progress);
                    Invalidate();
                }
                return true;
            }
            return base.OnTouchEvent(e);
        }
    }
}
