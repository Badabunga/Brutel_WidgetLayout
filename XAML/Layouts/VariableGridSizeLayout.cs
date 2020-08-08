using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using VirtualizingLayout = Microsoft.UI.Xaml.Controls.VirtualizingLayout;
using VirtualizingLayoutContext = Microsoft.UI.Xaml.Controls.VirtualizingLayoutContext;

namespace WidgetLayout.XAML.Layouts
{
    public class VariableGridSizeLayout : VirtualizingLayout
    {
        public double ColumnSpacing { get; set; }

        public double RowSpacing { get; set; }

        public double Width { get; set; }

        public double UnifedRowWidthLimit { get; set; }

        int m_firstIndex = 0;
        int m_lastIndex = 0;
        double m_lastAvailableWidth = 0.0;
        List<double> m_columnOffsets = new List<double>();
        List<Rect> m_cachedBounds = new List<Rect>();
        Dictionary<int, List<Rect>> _rowCachedBounds = new Dictionary<int, List<Rect>>();
        List<double> _rowHeight = new List<double>();
        List<double> _rowXOffSet = new List<double>();

        private bool cachedBoundsInvalid = false;


        protected override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
        {
            // The data collection has changed, so the bounds of all the indices are not valid anymore. 
            // We need to re-evaluate all the bounds and cache them during the next measure.
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Replace:

                    this.cachedBoundsInvalid = true;
                    this.InvalidateArrange();
                    break;

                default:
                    this._rowCachedBounds.Clear();
                    this._rowHeight.Clear();
                    this._rowXOffSet.Clear();
                    this.m_cachedBounds.Clear();
                    this.m_firstIndex = this.m_lastIndex = 0;
                    this.cachedBoundsInvalid = true;
                    this.InvalidateMeasure();
                    break;

            }

        }

        protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            var realWidth = 0d;

            if (this.Width == 0 || this.Width > availableSize.Width)
            {
                realWidth = availableSize.Width;
            }
            else
            {
                realWidth = this.Width;
            }


            var viewport = context.RealizationRect;


            // Update Logic when Items are still here
            if (availableSize.Width != this.m_lastAvailableWidth || this.cachedBoundsInvalid)
            {
                this.UpdateCachedBounds(context, availableSize, realWidth);
                this.m_lastAvailableWidth = availableSize.Width;

                if (this.UnifedRowWidthLimit != 0 && this.UnifedRowWidthLimit > this.m_lastAvailableWidth)
                {
                    this.UnifyRowWidth(context, availableSize, this.UnifedRowWidthLimit);
                }
            }



            this.m_firstIndex = this.GetStartIndex(viewport);
            int currentIndex = this.m_firstIndex;
            int currentRowIndex = 0;
            double nextYOffset = -1.0;
            double nextXOffset = -1.0;
            double diff = 0d;
            var currentCachedItemCount = this._rowCachedBounds.Values.Count;

            if (this.cachedBoundsInvalid)
            {
                // Measure items from start index to when we hit the end of the viewport.
                while (currentIndex < context.ItemCount && nextYOffset < viewport.Bottom)
                {
                    var child = context.GetOrCreateElementAt(currentIndex);

                    child.Measure(new Size(realWidth, availableSize.Height));

                    if (currentIndex >= currentCachedItemCount)
                    {
                        // We do not have bounds for this index. Lay it out and cache it.
                        currentRowIndex = this.GetIndexOfLowestRow(child, realWidth, out nextXOffset);
                        nextYOffset = currentRowIndex == 0 ? 0 : this._rowHeight[currentRowIndex];
                        //  int columnIndex = GetIndexOfLowestColumn(m_columnOffsets, child, realWidth, out nextYOffset);
                        var currentBound = new Rect(nextXOffset, nextYOffset, child.DesiredSize.Width, child.DesiredSize.Height);

                        if (this._rowCachedBounds.ContainsKey(currentRowIndex))
                        {
                            this._rowCachedBounds[currentRowIndex].Add(currentBound);
                        }

                        this.UpdateRowHeight(child.DesiredSize.Height, currentRowIndex);

                        this._rowXOffSet[currentRowIndex] += child.DesiredSize.Width + this.ColumnSpacing;
                    }

                    this.m_lastIndex = currentRowIndex;
                    currentIndex++;
                }

                if (this.UnifedRowWidthLimit != 0 && this.UnifedRowWidthLimit > this.m_lastAvailableWidth)
                {
                    this.UnifyRowWidth(context, availableSize, this.UnifedRowWidthLimit);
                }

                this.cachedBoundsInvalid = false;
            }


            var extent = this.GetExtentSize(availableSize);
            return extent;
        }

        private void UnifyRowWidth(VirtualizingLayoutContext context, Size availableSize, double unifedRowWidthLimit)
        {
            if (this._rowCachedBounds.Count > 0)
            {

                double previousWidthDiff = 0;

                var biggestXOffSet = this._rowXOffSet.Max();

                for (int row = 0; row < this._rowCachedBounds.Count; row++)
                {
                    var rowItems = this._rowCachedBounds[row];
                    var currentXOffSet = 0d;
                    var nextRowIndex = row + 1;
                    var xOffsettDif = 0d;
                    var currentMaxChildHeight = rowItems.Count > 0 ? rowItems.Max(x => x.Height) : this._rowHeight[row];
                    var rowBaseHeight = this._rowHeight[row];

                    if (this._rowXOffSet[row] < biggestXOffSet)
                    {
                        var diff = biggestXOffSet - this._rowXOffSet[row];

                        xOffsettDif = diff / rowItems.Count;
                    }

                    // Childs werden neu berechnet Höhe , Breite
                    for (int index = 0; index < rowItems.Count; index++)
                    {
                        var oldRectangle = rowItems[index];
                        var childWidth = oldRectangle.Width;

                        var currentDesiredWidth = childWidth + xOffsettDif;

                        var childIndex = this.FindIndexOfRowChild(row, index);
                        var child = context.GetOrCreateElementAt(childIndex);
                        child.Measure(new Size(currentDesiredWidth, availableSize.Height));

                        var childMaxWidth = (double)child.GetValue(FrameworkElement.MaxWidthProperty);
                        if (currentDesiredWidth <= childMaxWidth)
                        {
                            oldRectangle.Width = currentDesiredWidth;

                            oldRectangle.X = currentXOffSet;
                            rowItems[index] = oldRectangle;
                        }

                        currentXOffSet += currentDesiredWidth + this.ColumnSpacing;
                    }

                    this._rowXOffSet[row] = currentXOffSet;
                }
            }
        }

        private void UpdateRowHeight(double currentHeight, int currentRowIndex)
        {
            if (this.CheckIfChildIsBiggerThanCurrentMaxRowHeight(currentHeight, currentRowIndex, out var heightDiff))
            {
                //TODO: RowSpacing bei Spalten wechsel einbauen
                //m_columnOffsets[columnIndex] += child.DesiredSize.Height /*+ this.RowSpacing ?? 0*/;

                for (int i = currentRowIndex + 1; i < this._rowHeight.Count; i++)
                {
                    this._rowHeight[i] += heightDiff;
                }


            }
        }

        protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            if (this._rowCachedBounds.Count > 0)
            {
                for (int row = this.m_firstIndex; row <= this._rowCachedBounds.Count - 1; row++)
                {
                    var rowItems = this._rowCachedBounds[row];

                    for (int index = 0; index < rowItems.Count; index++)
                    {
                        var childIndex = this.FindIndexOfRowChild(row, index);
                        var child = context.GetOrCreateElementAt(childIndex);

                        var rect = rowItems[index];
                        child.Arrange(rect);
                    }
                }
            }
            return finalSize;
        }

        private void UpdateCachedBounds(VirtualizingLayoutContext context, Size availableSize, double realWidth)
        {
            int numColumns = (int)(availableSize.Width / realWidth);

            numColumns = numColumns == 0 ? 1 : numColumns;
            this.m_columnOffsets.Clear();

            for (int i = 0; i < numColumns; i++)
            {
                this.m_columnOffsets.Add(0);
            }

            if (this._rowCachedBounds.Count > 0)
            {

                double previousWidthDiff = 0;

                for (int row = 0; row < this._rowCachedBounds.Count; row++)
                {
                    var rowItems = this._rowCachedBounds[row];
                    var currentXOffSet = 0d;
                    var nextRowIndex = row + 1;
                    var currentMaxChildHeight = rowItems.Count > 0 ? rowItems.Max(x => x.Height) : this._rowHeight[row];
                    var rowBaseHeight = this._rowHeight[row];


                    // Childs werden neu berechnet Höhe , Breite
                    for (int index = 0; index < rowItems.Count; index++)
                    {
                        var childIndex = this.FindIndexOfRowChild(row, index);
                        var child = context.GetOrCreateElementAt(childIndex);
                        child.Measure(new Size(realWidth, availableSize.Height));

                        var oldRectangle = rowItems[index];

                        var currentChildHeight = rowBaseHeight + child.DesiredSize.Height + this.RowSpacing;

                        if (nextRowIndex < this._rowHeight.Count && (this._rowHeight[nextRowIndex] < currentChildHeight))
                        {
                            var heightDiff = currentChildHeight - this._rowHeight[nextRowIndex];

                            for (int nextRows = nextRowIndex; nextRows < this._rowHeight.Count; nextRows++)
                            {
                                this._rowHeight[nextRows] += heightDiff;
                            }
                        }

                        if (nextRowIndex < this._rowHeight.Count && (this._rowHeight[nextRowIndex] > currentChildHeight) && currentMaxChildHeight == oldRectangle.Height)
                        {
                            var heightDiff = currentChildHeight - this._rowHeight[nextRowIndex];

                            for (int nextRows = nextRowIndex; nextRows < this._rowHeight.Count; nextRows++)
                            {
                                this._rowHeight[nextRows] += heightDiff;
                            }
                        }

                        oldRectangle.X = currentXOffSet;
                        oldRectangle.Width = child.DesiredSize.Width;
                        oldRectangle.Height = child.DesiredSize.Height;
                        oldRectangle.Y = row == 0 ? 0d : this._rowHeight[row];
                        rowItems[index] = oldRectangle;
                        currentXOffSet += child.DesiredSize.Width + this.ColumnSpacing;
                    }

                    this._rowXOffSet[row] = currentXOffSet;

                    // Letztes Item in die nächste Zeile
                    if (this._rowXOffSet[row] > availableSize.Width && rowItems.Count > 1)
                    {
                        var lastChildIndex = rowItems.Count - 1;
                        var lastChildCurrentRowIndex = this.FindIndexOfRowChild(row, lastChildIndex);

                        var lastChild = context.GetOrCreateElementAt(lastChildCurrentRowIndex);

                        var rowHeight = 0d;

                        if (nextRowIndex < this._rowHeight.Count - 1)
                        {
                            rowHeight = this._rowHeight[nextRowIndex];
                        }
                        else
                        {
                            this.AddNewRow(lastChild);
                        }

                        var newBound = new Rect(0d, rowHeight, lastChild.DesiredSize.Width, lastChild.DesiredSize.Height);

                        if (this._rowCachedBounds.ContainsKey(nextRowIndex))
                        {
                            this._rowCachedBounds[nextRowIndex].Insert(0, newBound);
                        }

                        this.UpdateRowHeight(lastChild.DesiredSize.Height, nextRowIndex);
                        this._rowXOffSet[row] -= lastChild.DesiredSize.Width + this.ColumnSpacing;
                        rowItems.RemoveAt(lastChildIndex);


                    }


                    // Hinzufügen zur aktuellen Zeile
                    else
                    {
                        if (this._rowCachedBounds.TryGetValue(nextRowIndex, out var childs) && childs.Count > 0)
                        {
                            var nextRowChildIndex = this.FindIndexOfRowChild(nextRowIndex, 0);
                            var nextRowChild = context.GetOrCreateElementAt(nextRowChildIndex);

                            if (this.FirstChildFromNextRowFitsIntoCurrentRow(nextRowChild, realWidth, row))
                            {
                                var child = childs[0];

                                var rowHeigt = row == 0 ? 0 : this._rowHeight[row];
                                var newBound = new Rect(this._rowXOffSet[row], rowHeigt, nextRowChild.DesiredSize.Width, nextRowChild.DesiredSize.Height);
                                rowItems.Add(newBound);
                                this._rowXOffSet[row] += nextRowChild.DesiredSize.Width + this.ColumnSpacing;

                                this._rowXOffSet[nextRowIndex] -= nextRowChild.DesiredSize.Width + this.ColumnSpacing;

                                if (childs.Count - 1 != 0)
                                {
                                    var maxHeight = childs.Max(x => x.Height);

                                    if (maxHeight <= nextRowChild.DesiredSize.Height)
                                    {
                                        var childsWithoutNextRowChild = new List<Rect>(childs);

                                        childsWithoutNextRowChild.RemoveAt(0);

                                        maxHeight = childsWithoutNextRowChild.Max(x => x.Height);

                                        var diff = nextRowChild.DesiredSize.Height - maxHeight;

                                        for (int i = nextRowIndex; i < this._rowHeight.Count; i++)
                                        {
                                            this._rowHeight[i] -= diff;
                                        }
                                    }

                                    this.UpdateRowHeight(nextRowChild.DesiredSize.Height, row);

                                }

                                childs.RemoveAt(0);

                                if (nextRowIndex >= this._rowCachedBounds.Count - 1)
                                {
                                    this.DeleteRow(nextRowIndex);
                                }

                            }
                        }
                    }
                }
                this.cachedBoundsInvalid = false;
            }


        }


        private int FindIndexOfRowChild(int searchedRow, int childIndex)
        {
            var foundIndex = 0;

            if (this._rowCachedBounds.ContainsKey(searchedRow))
            {
                for (int row = 0; row <= searchedRow; row++)
                {
                    var currentCount = this._rowCachedBounds[row].Count;

                    if (row == searchedRow)
                    {
                        for (int lastChilds = 0; lastChilds < childIndex; lastChilds++)
                        {
                            foundIndex++;
                        }
                    }
                    else
                    {
                        for (int child = 0; child < currentCount; child++)
                        {
                            foundIndex++;
                        }
                    }
                }
            }

            return foundIndex;
        }


        private bool CheckIfChildIsBiggerThanCurrentMaxRowHeight(double height, int currentRowIndex, out double heighDiff)
        {
            var retValue = false;
            heighDiff = 0;

            if ((this._rowHeight[currentRowIndex] - this.RowSpacing) < height && currentRowIndex != 0)
            {
                if (this._rowHeight[currentRowIndex] == 0)
                {
                    heighDiff = height;
                }
                else
                {
                    heighDiff = height - (this._rowHeight[currentRowIndex] - this.RowSpacing);
                }

                retValue = true;
            }

            return retValue;
        }

        private bool FirstChildFromNextRowFitsIntoCurrentRow(UIElement uIElement, double realWidth, int currentRow)
        {
            var currentRowXOffSet = this._rowXOffSet[currentRow];

            if (currentRowXOffSet + uIElement.DesiredSize.Width + this.ColumnSpacing < realWidth)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private int FindRowOfCurrentChildIndex(int childIndex)
        {
            var childCount = 0;
            var retValue = 0;

            for (int row = 0; row < this._rowCachedBounds.Count; row++)
            {
                for (int bound = 0; bound < this._rowCachedBounds[row].Count; bound++)
                {
                    if (childCount == childIndex)
                    {
                        retValue = row;
                    }

                    childCount++;
                }
            }
            return retValue;
        }

        private int GetStartIndex(Rect viewport)
        {
            int startIndex = 0;
            if (this._rowCachedBounds.Count == 0)
            {
                startIndex = 0;
            }
            else
            {
                // find first index that intersects the viewport
                // perhaps this can be done more efficiently than walking
                // from the start of the list.
                for (int row = 0; row < this._rowCachedBounds.Count; row++)
                {
                    var rowItems = this._rowCachedBounds[row];

                    for (int i = 0; i < rowItems.Count; i++)
                    {
                        var currentBounds = rowItems[i];
                        if (currentBounds.Y < viewport.Bottom &&
                            currentBounds.Bottom > viewport.Top)
                        {
                            return row;
                        }
                    }
                }
            }

            return startIndex;
        }

        private int GetIndexOfLowestColumn(List<double> columnOffsets, UIElement currentChild, double availableWidth, out double lowestOffset)
        {
            int lowestIndex = 0;
            lowestOffset = columnOffsets[lowestIndex];

            for (int index = 0; index < columnOffsets.Count; index++)
            {
                var currentXOffSet = this._rowXOffSet[index];

                // Wenn aktuelle Zeilenbreite Platz hat für Child
                if (currentXOffSet + currentChild.DesiredSize.Width < availableWidth)
                {
                    var currentOffset = columnOffsets[index];
                    if (lowestOffset > currentOffset)
                    {
                        lowestOffset = currentOffset;
                        lowestIndex = index;
                    }

                    break;
                }
            }

            return lowestIndex;
        }

        private int GetIndexOfLowestRow(UIElement child, double realWidth, out double nextXOffset)
        {
            int? foundIndex = null;
            nextXOffset = 0;

            if (this._rowXOffSet.Count != 0)
            {
                nextXOffset = this._rowXOffSet[0];
                var columnSpacing = this.ColumnSpacing;

                for (int index = this._rowXOffSet.Count - 1; index >= 0; index--)
                {
                    var currentXOffSet = this._rowXOffSet[index];

                    if (currentXOffSet != 0)
                    {
                        if (currentXOffSet + child.DesiredSize.Width + columnSpacing < realWidth)
                        {
                            nextXOffset = currentXOffSet;
                            foundIndex = index;
                            break;
                        }
                    }
                }
            }

            if (!foundIndex.HasValue)
            {
                nextXOffset = 0;
                foundIndex = this._rowCachedBounds.Count == 0 ? 0 : this._rowCachedBounds.Count - 1;
                this.AddNewRow(child);

            }

            return foundIndex.Value;
        }

        private void AddNewRow(UIElement child)
        {
            this._rowXOffSet.Add(0);
            var currentRow = this._rowXOffSet.Count - 1;
            this._rowCachedBounds.Add(currentRow, new List<Rect>());

            if (currentRow == 0)
            {
                this._rowHeight.Add(0);
                this._rowXOffSet.Add(0);
                this._rowCachedBounds.Add(currentRow + 1, new List<Rect>());
            }

            var lastMaxHeight = this.GetNextRowHeight();
            this._rowHeight.Add(lastMaxHeight + child.DesiredSize.Height + this.RowSpacing);
        }

        private void DeleteRow(int nextRowIndex)
        {
            this._rowXOffSet.RemoveAt(nextRowIndex);
            this._rowHeight.RemoveAt(nextRowIndex);
            this._rowCachedBounds.Remove(nextRowIndex);
        }


        private double GetNextRowHeight()
        {
            var retValue = 0d;

            for (int i = 0; i < this._rowHeight.Count; i++)
            {
                retValue += this._rowHeight[i];
            }
            //retValue = this._rowHeight[this._rowHeight.Count - 1];

            return retValue;
        }


        private Size GetExtentSize(Size availableSize)
        {
            var largestColumnOffset = 0d;
            if (this._rowXOffSet.Count > 0)
            {
                largestColumnOffset = this._rowXOffSet[0];
                for (int index = 0; index < this._rowXOffSet.Count; index++)
                {
                    var currentOffset = this._rowXOffSet[index];
                    if (largestColumnOffset < currentOffset)
                    {
                        largestColumnOffset = currentOffset;
                    }
                }
            }

            return new Size(availableSize.Width, largestColumnOffset);
        }
    }

}
