﻿namespace UIBuddy.UI.ScrollView;

/// <summary>
/// A data source for a ScrollPool.
/// </summary>
public interface ICellPoolDataSource<T> where T : ICell
{
    int ItemCount { get; }

    void OnCellBorrowed(T cell);

    void SetCell(T cell, int index);
}