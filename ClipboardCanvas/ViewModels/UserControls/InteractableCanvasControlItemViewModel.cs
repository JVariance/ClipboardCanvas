﻿using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

using ClipboardCanvas.DataModels;
using ClipboardCanvas.DataModels.PastedContentDataModels;
using ClipboardCanvas.Helpers.SafetyHelpers;
using ClipboardCanvas.Models;
using ClipboardCanvas.ModelViews;
using ClipboardCanvas.ViewModels.ContextMenu;
using ClipboardCanvas.Helpers.Filesystem;
using ClipboardCanvas.Helpers;
using ClipboardCanvas.EventArguments.InfiniteCanvasEventArgs;

namespace ClipboardCanvas.ViewModels.UserControls
{
    public class InteractableCanvasControlItemViewModel : ObservableObject, IInteractableCanvasControlItemModel, IDisposable
    {
        #region Private Members

        private IInteractableCanvasControlView _view;

        private BaseContentTypeModel _contentType;

        private CancellationToken _cancellationToken;

        #endregion

        #region Properties

        public List<BaseMenuFlyoutItemViewModel> ContextMenuItems { get; private set; }

        public IReadOnlyCanvasPreviewModel ReadOnlyCanvasPreviewModel { get; set; }

        public ICollectionModel CollectionModel { get; set; }

        public CanvasItem CanvasItem { get; private set; }

        private bool _IsPastedAsReference;
        public bool IsPastedAsReference
        {
            get => _IsPastedAsReference;
            set
            {
                if (SetProperty(ref _IsPastedAsReference, value))
                {
                    OnPropertyChanged(nameof(DeleteItemText));
                }
            }
        }

        public string DeleteItemText
        {
            get => IsPastedAsReference ? "Delete reference" : "Delete item";
        }

        private string _DisplayName;
        public string DisplayName
        {
            get => _DisplayName;
            set => SetProperty(ref _DisplayName, value);
        }

        public Vector2 ItemPosition
        {
            get => _view.GetItemPosition(this);
            set => _view.SetItemPosition(this, value);
        }

        /// <summary>
        /// The horizontal position
        /// </summary>
        public float XPos
        {
            get => ItemPosition.X;
            set => ItemPosition = new Vector2(value, ItemPosition.Y);
        }

        /// <summary>
        /// The vertical position
        /// </summary>
        public float YPos
        {
            get => ItemPosition.Y;
            set => ItemPosition = new Vector2(ItemPosition.X, value);
        }

        #endregion

        #region Events

        public event EventHandler<InfiniteCanvasItemRemovalRequestedEventArgs> OnInfiniteCanvasItemRemovalRequestedEvent;

        #endregion

        #region Commands

        public ICommand OpenFileCommand { get; private set; }

        public ICommand SetDataToClipboardCommand { get; private set; }

        public ICommand OpenContainingFolderCommand { get; private set; }

        public ICommand OpenReferenceContainingFolderCommand { get; private set; }

        public ICommand DeleteItemCommand { get; private set; }

        #endregion

        #region Constructor

        public InteractableCanvasControlItemViewModel(IInteractableCanvasControlView view, ICollectionModel collectionModel, BaseContentTypeModel contentType, CanvasItem canvasItem, CancellationToken cancellationToken)
        {
            this._view = view;
            this.CollectionModel = collectionModel;
            this._contentType = contentType;
            this.CanvasItem = canvasItem;
            this._cancellationToken = cancellationToken;

            // Create commands
            OpenFileCommand = new AsyncRelayCommand(OpenFile);
            SetDataToClipboardCommand = new AsyncRelayCommand(SetDataToClipboard);
            OpenContainingFolderCommand = new AsyncRelayCommand(OpenContainingFolder);
            OpenReferenceContainingFolderCommand = new AsyncRelayCommand(() => OpenContainingFolder(false));
            DeleteItemCommand = new AsyncRelayCommand(DeleteItem);
        }

        #endregion

        #region Command Implementation

        private async Task OpenFile()
        {
            // TODO:
            await StorageHelpers.OpenFile(await CanvasItem.SourceItem);
        }

        private async Task SetDataToClipboard()
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetStorageItems(new List<IStorageItem>() { await CanvasItem.SourceItem });

            Clipboard.SetContent(dataPackage);
        }

        private async Task OpenContainingFolder()
        {
            await OpenContainingFolder(true);
        }

        private async Task OpenContainingFolder(bool checkForReference)
        {
            if (checkForReference)
            {
                await StorageHelpers.OpenContainingFolder(await CanvasItem.SourceItem);
            }
            else
            {
                await StorageHelpers.OpenContainingFolder(CanvasItem.AssociatedItem);
            }
        }

        private async Task DeleteItem()
        {
            SafeWrapperResult result = await CanvasHelpers.DeleteCanvasFile(CollectionModel, CanvasItem, false);

            if (result)
            {
                OnInfiniteCanvasItemRemovalRequestedEvent?.Invoke(this, new InfiniteCanvasItemRemovalRequestedEventArgs(this));
            }
        }

        #endregion

        private async Task InitializeItemName()
        {
            DisplayName = (await CanvasItem.SourceItem)?.Name ?? "Invalid file.";
        }

        public async Task InitializeItem()
        {
            await InitializeItemName();
        }

        public async Task<SafeWrapperResult> LoadContent(bool withLoadDelay = false)
        {
            if (withLoadDelay)
            {
                // Wait for control to load
                await Task.Delay(10);
            }

            SafeWrapperResult result = await ReadOnlyCanvasPreviewModel.TryLoadExistingData(CanvasItem, _contentType, _cancellationToken);
            IsPastedAsReference = result && CanvasItem.IsFileAsReference;

            return result;
        }

        public async Task<IReadOnlyList<IStorageItem>> GetDragData()
        {
            return new List<IStorageItem>() { await CanvasItem.SourceItem };
        }

        #region IDisposable

        public void Dispose()
        {
            _view = null;
        }

        #endregion
    }
}
