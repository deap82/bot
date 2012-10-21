using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using PixelLab.Common;
#if CONTRACTS_FULL
using System.Diagnostics.Contracts;
#else
using PixelLab.Contracts;
#endif

namespace PixelLab.Wpf
{
    public class AnimatingTilePanel : AnimatingPanel
    {
		public AnimatingTilePanel()
		{
			this._orientation = System.Windows.Controls.Orientation.Horizontal;
		}

        #region public properties

		#region Orientation
		public static readonly DependencyProperty OrientationProperty = StackPanel.OrientationProperty.AddOwner(typeof(AnimatingTilePanel), (PropertyMetadata)new FrameworkPropertyMetadata((object)Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure, new PropertyChangedCallback(AnimatingTilePanel.OnOrientationChanged)));
		private Orientation _orientation;

		public Orientation Orientation
		{
			get
			{
				return this._orientation;
			}
			set
			{
				this.SetValue(AnimatingTilePanel.OrientationProperty, (object)value);
			}
		}


		private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((AnimatingTilePanel)d)._orientation = (Orientation)e.NewValue;
		} 
		#endregion

		public static readonly DependencyProperty ItemMarginProperty =
			CreateDoubleDP("ItemMargin", 5, FrameworkPropertyMetadataOptions.AffectsMeasure, 0, double.PositiveInfinity, true);

		public double ItemMargin
		{
			get { return (double)GetValue(ItemMarginProperty); }
			set { SetValue(ItemMarginProperty, value); }
		}

		public static double GetItemMargin(DependencyObject element)
		{
			Contract.Requires<ArgumentNullException>(element != null);
			return (double)element.GetValue(ItemMarginProperty);
		}

		public static void SetItemMargin(DependencyObject element, double itemMargin)
		{
			Contract.Requires<ArgumentNullException>(element != null);
			element.SetValue(ItemMarginProperty, itemMargin);
		}


		public static readonly DependencyProperty ItemWidthProperty =
			CreateDoubleDP("ItemWidth", 50, FrameworkPropertyMetadataOptions.AffectsMeasure, 0, double.PositiveInfinity, true);
		
		public double ItemWidth
        {
            get { return (double)GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        public static double GetItemWidth(DependencyObject element)
        {
            Contract.Requires<ArgumentNullException>(element != null);
            return (double)element.GetValue(ItemWidthProperty);
        }

        public static void SetItemWidth(DependencyObject element, double itemWidth)
        {
            Contract.Requires<ArgumentNullException>(element != null);
            element.SetValue(ItemWidthProperty, itemWidth);
        }

        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        public static double GetItemHeight(DependencyObject element)
        {
            Contract.Requires<ArgumentNullException>(element != null);
            return (double)element.GetValue(ItemHeightProperty);
        }

        public static void SetItemHeight(DependencyObject element, double itemHeight)
        {
            Contract.Requires<ArgumentNullException>(element != null);
            element.SetValue(ItemHeightProperty, itemHeight);
        }

        public static readonly DependencyProperty ItemHeightProperty =
            CreateDoubleDP("ItemHeight", 50, FrameworkPropertyMetadataOptions.AffectsMeasure, 0, double.PositiveInfinity, true);

        #endregion

        #region protected override

        protected override Size MeasureOverride(Size availableSize)
        {
            onPreApplyTemplate();

            Size theChildSize = getItemSize();

            foreach (UIElement child in Children)
            {
                child.Measure(theChildSize);
            }

            int childrenPerRow;

            // Figure out how many children fit on each row
            if (availableSize.Width == Double.PositiveInfinity)
            {
                childrenPerRow = this.Children.Count;
            }
            else
            {
                childrenPerRow = Math.Max(1, (int)Math.Floor(availableSize.Width / this.ItemWidth));
            }

            // Calculate the width and height this results in
            double width = childrenPerRow * this.ItemWidth;
            double height = this.ItemHeight * (Math.Floor((double)this.Children.Count / childrenPerRow) + 1);
            height = (height.IsValid()) ? height : 0;
            return new Size(width, height);
        }

        protected override sealed Size ArrangeOverride(Size finalSize)
        {
            // Calculate how many children fit on each row
			int childrenPerDivision;

			if (this.Orientation == System.Windows.Controls.Orientation.Horizontal)
			{
				childrenPerDivision = Math.Max(1, (int)Math.Floor(finalSize.Width / (this.ItemWidth + this.ItemMargin * 2)));
			}
			else
			{
				childrenPerDivision = Math.Max(1, (int)Math.Floor(finalSize.Height / (this.ItemHeight + this.ItemMargin * 2)));
			}

            Size theChildSize = getItemSize();

            for (int i = 0; i < this.Children.Count; i++)
            {
                // Figure out where the child goes
				Point newOffset = calculateChildOffset(i, childrenPerDivision,
                    							this.ItemWidth, this.ItemHeight,
                    							finalSize.Width, finalSize.Height, this.Children.Count, this.Orientation);
				

                ArrangeChild(Children[i], new Rect(newOffset, theChildSize));
            }

            m_arrangedOnce = true;
            return finalSize;
        }

        protected override Point ProcessNewChild(UIElement child, Rect providedBounds)
        {
            var startLocation = providedBounds.Location;
            if (m_arrangedOnce)
            {
                if (m_itemOpacityAnimation == null)
                {
                    m_itemOpacityAnimation = new DoubleAnimation()
                    {
                        From = 0,
                        Duration = new Duration(TimeSpan.FromSeconds(.5))
                    };
                    m_itemOpacityAnimation.Freeze();
                }

                child.BeginAnimation(UIElement.OpacityProperty, m_itemOpacityAnimation);
                startLocation -= new Vector(providedBounds.Width, 0);
            }
            return startLocation;
        }

        #endregion

        #region Implementation

        #region private methods

        private Size getItemSize() { return new Size(ItemWidth, ItemHeight); }

        private void bindToParentItemsControl(DependencyProperty property, DependencyObject source)
        {
            if (DependencyPropertyHelper.GetValueSource(this, property).BaseValueSource == BaseValueSource.Default)
            {
                Binding binding = new Binding();
                binding.Source = source;
                binding.Path = new PropertyPath(property);
                base.SetBinding(property, binding);
            }
        }

        private void onPreApplyTemplate()
        {
            if (!m_appliedTemplate)
            {
                m_appliedTemplate = true;

                DependencyObject source = base.TemplatedParent;
                if (source is ItemsPresenter)
                {
                    source = TreeHelpers.FindParent<ItemsControl>(source);
                }

                if (source != null)
                {
                    bindToParentItemsControl(ItemHeightProperty, source);
                    bindToParentItemsControl(ItemWidthProperty, source);
                }
            }
        }

        // Given a child index, child size and children per row, figure out where the child goes
        private static Point calculateChildOffset(
            int index,
            int childrenPerDivision,
            double itemWidth,
            double itemHeight,
            double panelWidth,
			double panelHeight,
            int totalChildren,
			Orientation orientation)
        {
            double fudge = 0;
            if (totalChildren > childrenPerDivision)
            {
				double itemSide = orientation == Orientation.Horizontal ? itemWidth : itemHeight;
				double panelSide = orientation == Orientation.Horizontal ? panelWidth : panelHeight;
                fudge = (panelSide - childrenPerDivision * itemSide) / childrenPerDivision;
                Debug.Assert(fudge >= 0);
            }

			int row, column;

			if (orientation == Orientation.Horizontal)
			{
				row = index / childrenPerDivision;
				column = index % childrenPerDivision;
			}
			else
			{
				row = index % childrenPerDivision;
				column = index / childrenPerDivision;
			}

            return new Point(.5 * fudge + column * (itemWidth + fudge), row * itemHeight);
        }

        #endregion

        private bool m_appliedTemplate;
        private bool m_arrangedOnce;
        private DoubleAnimation m_itemOpacityAnimation;

        #endregion
    } //*** class AnimatingTilePanel
}
