﻿using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace EPPlus.DataExtractor
{
    internal interface ICollectionColumnDataExtractor<TRow>
        where TRow : class, new()
    {
        void SetPropertyValue(TRow dataInstance, int row, ExcelRange cellRange);
    }

    internal class CollectionColumnDataExtractor<TRow, TCollection, TCollectionItem, THeadValue, TRowValue>
        : ICollectionColumnDataExtractor<TRow>
        where TCollection : class, ICollection<TCollectionItem>, new()
        where TRow : class, new()
        where TCollectionItem : class, new()
    {
        private readonly int headerRow;
        private readonly string initialColumn;
        private readonly string finalColumn;
        private readonly Action<TRow, TCollection> setCollectionProperty;
        private readonly IRowDataExtractor<TCollectionItem> collectionItemHeadPropertySetter;
        private readonly IRowDataExtractor<TCollectionItem> collectionItemRowPropertySetter;

        public CollectionColumnDataExtractor(Expression<Func<TRow, TCollection>> collectionPropertyExpr,
            Expression<Func<TCollectionItem, THeadValue>> collectionItemHeaderProperty, int headerRow,
            Expression<Func<TCollectionItem, TRowValue>> collectionItemRowProperty,
            string initialColumn, string finalColumn)
        {
            this.headerRow = headerRow;
            this.initialColumn = initialColumn;
            this.finalColumn = finalColumn;
            this.setCollectionProperty = collectionPropertyExpr.CreatePropertyValueSetterAction();
            this.collectionItemHeadPropertySetter = new RowDataExtractor<TCollectionItem, THeadValue>(collectionItemHeaderProperty);
            this.collectionItemRowPropertySetter = new RowDataExtractor<TCollectionItem, TRowValue>(collectionItemRowProperty);
        }

        private static Action<TRow, TCollection> CreateSetCollectionPropertyAction(Expression<Func<TRow, TCollection>> collectionPropertyExpr)
        {
            var parameter = Expression.Parameter(typeof(TCollection));
            var setPropActionExpression = Expression.Lambda<Action<TRow, TCollection>>(
                Expression.Assign(collectionPropertyExpr.Body, parameter),
                collectionPropertyExpr.Parameters[0], parameter);

            return setPropActionExpression.Compile();
        }

        public void SetPropertyValue(TRow dataInstance, int row, ExcelRange cellRange)
        {
            var collection = new TCollection();
            
            foreach (var cell in cellRange[initialColumn + row + ":" + finalColumn + row])
            {
                var collectionItem = new TCollectionItem();

                // cell here will be a single cell, always.
                // So I get the column from that cell in order to obtain the header.
                int column = cell.Start.Column;
                this.collectionItemHeadPropertySetter.SetPropertyValue(collectionItem, cellRange[this.headerRow, column]);

                this.collectionItemRowPropertySetter.SetPropertyValue(collectionItem, cell);

                collection.Add(collectionItem);
            }

            this.setCollectionProperty(dataInstance, collection);
        }
    }
}
