using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlowExec
{
    public partial class FluentImage : UserControl
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ImageSource), typeof(FluentImage),
                new PropertyMetadata(null, OnSourceChanged));

        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register("AnimationDuration", typeof(double), typeof(FluentImage),
                new PropertyMetadata(0.3));

        public ImageSource Source
        {
            get => (ImageSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public double AnimationDuration
        {
            get => (double)GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register("Stretch", typeof(Stretch), typeof(FluentImage),
                new PropertyMetadata(Stretch.Uniform));

        public FluentImage()
        {
            InitializeComponent();
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FluentImage)d;
            if (e.NewValue != null)
                control.TransitionToImage(e.NewValue as ImageSource);
        }

        public void TransitionToImage(ImageSource newImage)
        {
            if (newImage == null) return;

            // 设置新图片到顶层控件
            NextImage.Source = newImage;

            // 创建淡入动画 (新图片)
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(AnimationDuration),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            // 创建淡出动画 (旧图片)
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(AnimationDuration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // 动画完成后更新底层图片并重置状态
            fadeIn.Completed += (s, e) =>
            {
                CurrentImage.Source = newImage;  // 更新底层图片
                NextImage.Opacity = 0;           // 重置顶层透明度
            };

            // 同时启动两个动画
            NextImage.BeginAnimation(Image.OpacityProperty, fadeIn);
            CurrentImage.BeginAnimation(Image.OpacityProperty, fadeOut);
        }
    }
}
