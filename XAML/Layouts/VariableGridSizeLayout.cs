using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.Web.Http;
using VirtualizingLayout = Microsoft.UI.Xaml.Controls.VirtualizingLayout;
using VirtualizingLayoutContext = Microsoft.UI.Xaml.Controls.VirtualizingLayoutContext;

namespace WidgetLayout.XAML.Layouts
{
    public class VariableGridSizeLayout : VirtualizingLayout
    {
        public double? ColumnSpacing { get; set; }

        public double? RowSpacing { get; set; }

        public double? Width { get; set; }


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
            this._rowCachedBounds.Clear();
            this._rowHeight.Clear();
            this._rowXOffSet.Clear();
            this.m_cachedBounds.Clear();
            this.m_firstIndex = this.m_lastIndex = 0;
            this.cachedBoundsInvalid = true;
            this.InvalidateMeasure();
        }

        protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            var realWidth = 0d;

            if (!this.Width.HasValue || this.Width.Value > availableSize.Width)
            {
                realWidth = availableSize.Width;
            }
            else
            {
                realWidth = this.Width.Value;
            }


            var viewport = context.RealizationRect;


            // Update Logic when Items are still here
            if (availableSize.Width != this.m_lastAvailableWidth || this.cachedBoundsInvalid)
            {
                UpdateCachedBounds(context, availableSize, realWidth);
                this.m_lastAvailableWidth = availableSize.Width;
            }


            // Initialize column offsets
            int numColumns = (int)(availableSize.Width / realWidth);
            if (this.m_columnOffsets.Count == 0)
            {
                for (int i = 0; i < numColumns; i++)
                {
                    this.m_columnOffsets.Add(0);
                    this._rowXOffSet.Add(0);
                    this._rowHeight.Add(0);
                }
            }

            this.m_firstIndex = this.GetStartIndex(viewport);
            int currentIndex = this.m_firstIndex;
            int currentRowIndex = 0;
            double nextYOffset = -1.0;
            double nextXOffset = -1.0;
            double diff = 0d;


            // Measure items from start index to when we hit the end of the viewport.
            while (currentIndex < context.ItemCount && nextYOffset < viewport.Bottom)
            {
                var child = context.GetOrCreateElementAt(currentIndex);

                child.Measure(new Size(realWidth, availableSize.Height));

                if (currentIndex >= this.m_cachedBounds.Count)
                {
                    // We do not have bounds for this index. Lay it out and cache it.
                    currentRowIndex = this.GetIndexOfLowestRow(child, realWidth, out nextXOffset);
                    nextYOffset = currentRowIndex == 0 ? 0 : this._rowHeight[currentRowIndex];
                    //  int columnIndex = GetIndexOfLowestColumn(m_columnOffsets, child, realWidth, out nextYOffset);
                    var currentBound = new Rect(nextXOffset, nextYOffset, child.DesiredSize.Width, child.DesiredSize.Height);
                    this.m_cachedBounds.Add(currentBound);
                    if (this._rowCachedBounds.ContainsKey(currentRowIndex))
                    {
                        this._rowCachedBounds[currentRowIndex].Add(currentBound);
                    }
                    else
                    {
                        this._rowCachedBounds.Add(currentRowIndex, new List<Rect> { currentBound });
                    }

                    if (this.CheckIfChildIsBiggerThanCurrentMaxRowHeight(child, currentRowIndex, out var heightDiff))
                    {
                        //TODO: RowSpacing bei Spalten wechsel einbauen
                        //m_columnOffsets[columnIndex] += child.DesiredSize.Height /*+ this.RowSpacing ?? 0*/;
                        this._rowHeight[currentRowIndex] += heightDiff;
                    }
                    this._rowXOffSet[currentRowIndex] += child.DesiredSize.Width + (this.ColumnSpacing ?? 0);
                }
                else
                {
                    if (currentIndex + 1 == this.m_cachedBounds.Count)
                    {
                        // Last element. Use the next offset.
                        this.GetIndexOfLowestColumn(this.m_columnOffsets, child, realWidth, out nextYOffset);
                    }
                    else
                    {
                        nextYOffset = this.m_cachedBounds[currentIndex + 1].Top + this.RowSpacing ?? 0;
                    }

                    //if (child.DesiredSize.Height != this.m_cachedBounds[currentIndex].Height)
                    //{
                    //    var rect = this.m_cachedBounds[currentIndex];
                    //    rect.Height = child.DesiredSize.Height;
                    //    this.m_cachedBounds[currentIndex] = rect;
                    //    this.m_columnOffsets[currentIndex] += diff;
                    //}


                }


                this.m_lastIndex = currentIndex;
                currentIndex++;
            }

            var extent = this.GetExtentSize(availableSize);
            return extent;
        }


        protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            if (this.m_cachedBounds.Count > 0)
            {
                for (int index = this.m_firstIndex; index <= this.m_lastIndex; index++)
                {
                    var child = context.GetOrCreateElementAt(index);

                    var rect = this.m_cachedBounds[index];
                    child.Arrange(rect);
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
               
                // Childs werden neu berechnet Höhe , Breite
                for (int index = 0; index < this.m_cachedBounds.Count; index++)
                {
                    var child = context.GetOrCreateElementAt(index);
                    child.Measure(new Size(realWidth, availableSize.Height));

                    var oldRectangle = this.m_cachedBounds[index];

                    var findRow = this.FindRowOfCurrentChildIndex(index);


                    if (findRow + 1 < this._rowHeight.Count)
                    {
                        if ((this._rowHeight[findRow + 1] - (this.RowSpacing ?? 0)) < child.DesiredSize.Height || (this._rowHeight[findRow + 1] - (this.RowSpacing ?? 0)) > child.DesiredSize.Height)
                        {
                            var diff = child.DesiredSize.Height - oldRectangle.Height;

                            for (int rows = findRow + 1; rows < this._rowHeight.Count; rows++)
                            {
                                this._rowHeight[rows] += diff;
                            }
                        }
                    }

                  

                   // oldRectangle.X += currentXDiff;
                    oldRectangle.Width = child.DesiredSize.Width;
                    oldRectangle.Height = child.DesiredSize.Height;
                    oldRectangle.Y = findRow == 0 ? 0d : this._rowHeight[findRow];
                    this.m_cachedBounds[index] = oldRectangle;
                    
                    //int columnIndex = GetIndexOfLowestColumn(m_columnOffsets, out var nextOffset);
                    //var oldHeight = m_cachedBounds[index].Height;

                    //m_cachedBounds[index] = new Rect(columnIndex * realWidth, nextOffset, realWidth, oldHeight);
                    //m_columnOffsets[columnIndex] += oldHeight + this.RowSpacing ?? 0;
                }


                // Row X OffSet wird anhand der neu berechneten Größe berechnet
                var childIndex = 0;
                var resizeAllRows = true;
                for (int row = 0; row < this._rowCachedBounds.Count; row++)
                {
                    var currentRowBounds = this._rowCachedBounds[row];
                    var newRowXOffset = 0d;

                    for (int bounds = 0; bounds < currentRowBounds.Count; bounds++)
                    {
                        var child = context.GetOrCreateElementAt(childIndex);
                        var oldBound = this.m_cachedBounds[childIndex];
                        oldBound.X = newRowXOffset;
                        this.m_cachedBounds[childIndex] = oldBound;
                        newRowXOffset += child.DesiredSize.Width + (this.ColumnSpacing ?? 0);
                        childIndex++;
                    }

                    this._rowXOffSet[row] = newRowXOffset;

                    // Letztes Item in die nächste Zeile
                    if(this._rowXOffSet[row] > availableSize.Width && currentRowBounds.Count > 1)
                    {
                        var currentListIndex = currentRowBounds.Count - 1;
                        var lastChildCurrentRowIndex = this.FindIndexOfRowChild(row, currentListIndex);

                        var lastChild = context.GetOrCreateElementAt(lastChildCurrentRowIndex);
                        currentRowBounds.RemoveAt(currentListIndex);

                        var nextRowIndex = row + 1;

                        var rowHeight = 0d;

                        if((nextRowIndex) + 1 <= this._rowHeight.Count)
                        {
                            rowHeight = this._rowHeight[nextRowIndex];
                        }
                        else
                        {
                            rowHeight = this.GetNextRowHeight();
                            this._rowHeight.Add(rowHeight);
                            this._rowHeight.Add(rowHeight + lastChild.DesiredSize.Height + this.GetRowSpacing());
                        } 

                        var newBound = new Rect(0d, rowHeight, lastChild.DesiredSize.Width, lastChild.DesiredSize.Height);
                        this.m_cachedBounds[lastChildCurrentRowIndex] = newBound;

                        if (this._rowCachedBounds.ContainsKey(nextRowIndex))
                        {
                            this._rowCachedBounds[nextRowIndex].Insert(0, newBound);
                        }
                        else
                        {
                            this._rowCachedBounds.Add(nextRowIndex, new List<Rect>(){ newBound});
                            this._rowXOffSet.Add(lastChild.DesiredSize.Width + (this.ColumnSpacing ?? 0));
                        }

                        if(this.CheckIfChildIsBiggerThanCurrentMaxRowHeight(lastChild,nextRowIndex, out var heightDiff))
                        {
                            this._rowHeight[nextRowIndex] += heightDiff;
                        }

                        childIndex -= 1;
                    }


                    // Hinzufügen zur aktuellen Zeile
                    else
                    {
                        if (this._rowCachedBounds.TryGetValue(row + 1, out var childs) && childs.Count > 0)
                        {
                            var nextRowChildIndex = this.FindIndexOfRowChild(row + 1, 0);
                            var nextRowChild = context.GetOrCreateElementAt(nextRowChildIndex);

                            if (this.FirstChildFromNextRowFitsIntoCurrentRow(nextRowChild, realWidth, row))
                            {
                                var child = childs[0];
                                childs.RemoveAt(0);
                                var rowHeigt = row == 0 ? 0 : this._rowHeight[row];
                                var newBound = new Rect(this._rowXOffSet[row], rowHeigt, nextRowChild.DesiredSize.Width, nextRowChild.DesiredSize.Height);
                                this.m_cachedBounds[nextRowChildIndex] = newBound;
                                currentRowBounds.Add(newBound);
                            }
                        }
                    }

                 
                }
            }

            this.cachedBoundsInvalid = false;
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


        private bool CheckIfChildIsBiggerThanCurrentMaxRowHeight(UIElement child, int currentRowIndex, out double heighDiff)
        {
            var retValue = false;
            heighDiff = 0;
            if ((this._rowHeight[currentRowIndex] - (this.RowSpacing ?? 0)) < child.DesiredSize.Height)
            {
                if(this._rowHeight[currentRowIndex] == 0)
                {
                    heighDiff = child.DesiredSize.Height;
                }
                else
                {
                    heighDiff = child.DesiredSize.Height - (this._rowHeight[currentRowIndex] - (this.RowSpacing ?? 0));
                }
               
                retValue = true;
            }

            return retValue;
        }

        private bool FirstChildFromNextRowFitsIntoCurrentRow(UIElement uIElement, double realWidth, int currentRow)
        {
            var currentRowXOffSet = this._rowXOffSet[currentRow];

            if (currentRowXOffSet + uIElement.DesiredSize.Width + (this.ColumnSpacing ?? 0) < realWidth)
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

            for(int row = 0; row < this._rowCachedBounds.Count; row++)
            {
                for(int bound = 0; bound < this._rowCachedBounds[row].Count; bound++)
                {
                    if(childCount == childIndex)
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
            if (this.m_cachedBounds.Count == 0)
            {
                startIndex = 0;
            }
            else
            {
                // find first index that intersects the viewport
                // perhaps this can be done more efficiently than walking
                // from the start of the list.
                for (int i = 0; i < this.m_cachedBounds.Count; i++)
                {
                    var currentBounds = this.m_cachedBounds[i];
                    if (currentBounds.Y < viewport.Bottom &&
                        currentBounds.Bottom > viewport.Top)
                    {
                        startIndex = i;
                        break;
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
                var columnSpacing = this.ColumnSpacing ?? 0;

                for (int index = this._rowXOffSet.Count - 1; index >= 0; index--)
                {
                    var currentXOffSet = this._rowXOffSet[index];

                    if (currentXOffSet + child.DesiredSize.Width + columnSpacing < realWidth)
                    {
                        nextXOffset = currentXOffSet;
                        foundIndex = index;
                        break;
                    }
                }
            }



            if (!foundIndex.HasValue)
            {
                this._rowXOffSet.Add(0);
                nextXOffset = 0;
                foundIndex = this._rowXOffSet.Count - 1;

                if (foundIndex.Value == 0)
                {
                    this._rowHeight.Add(0);
                }
                else
                {
                    var lastMaxHeight = this.GetNextRowHeight();
                    this._rowHeight.Add(lastMaxHeight);
                    this._rowHeight.Add(lastMaxHeight + child.DesiredSize.Height + this.GetRowSpacing());
                }
            }


            return foundIndex.Value;
        }


        private double GetNextRowHeight()
        {
            var retValue = 0d;

            retValue = this._rowHeight[this._rowHeight.Count - 1];
           

            //for (int i = 0; i < this._rowHeight.Count; i++)
            //{
            //    retValue += this._rowHeight[i];
            //}

            return retValue + (this.RowSpacing ?? 0);
        }


        private Size GetExtentSize(Size availableSize)
        {
            double largestColumnOffset = this.m_columnOffsets[0];
            for (int index = 0; index < this.m_columnOffsets.Count; index++)
            {
                var currentOffset = this.m_columnOffsets[index];
                if (largestColumnOffset < currentOffset)
                {
                    largestColumnOffset = currentOffset;
                }
            }

            return new Size(availableSize.Width, largestColumnOffset);
        }

        private double GetRowSpacing() => this.RowSpacing ?? 0;

    }

}
