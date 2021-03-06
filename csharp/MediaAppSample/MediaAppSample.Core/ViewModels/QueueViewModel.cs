﻿// The MIT License (MIT)
//
// Copyright (c) 2016 Microsoft. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using MediaAppSample.Core.Commands;
using MediaAppSample.Core.Data;
using MediaAppSample.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace MediaAppSample.Core.ViewModels
{
    public partial class QueueViewModel : ViewModelBase
    {
        #region Properties

        private QueueCollection _queue;
        /// <summary>
        /// Gets a list of content items in the queue
        /// </summary>
        public QueueCollection Queue
        {
            get { return _queue; }
            private set
            {
                if (this.SetProperty(ref _queue, value))
                {
                    // If the collection object changes, subscribe to the collection changed event so you can
                    // notify the QueuePreview property to update to show the latest queue items.
                    if (value != null)
                        value.CollectionChanged += (o, e) => this.UpdateQueueProperties();

                    this.UpdateQueueProperties();
                };
            }
        }

        private QueueCollection _QueuePreview;
        /// <summary>
        /// Gets the top 3 times in the queue collection.
        /// </summary>
        public QueueCollection QueuePreview
        {
            get { return _QueuePreview; }
            private set { this.SetProperty(ref _QueuePreview, value); }
        }

        private QueueModel _ResumeItem;
        public QueueModel ResumeItem
        {
            get { return _ResumeItem; }
            private set { this.SetProperty(ref _ResumeItem, value); }
        }

        private QueueModel _NextQueueItem;
        /// <summary>
        /// Gets the first item in the queue or null
        /// </summary>
        public QueueModel NextQueueItem
        {
            get { return _NextQueueItem; }
            private set { this.SetProperty(ref _NextQueueItem, value); }
        }

        private CommandBase _AddToQueueCommand = null;
        public CommandBase AddToQueueCommand
        {
            get { return _AddToQueueCommand ?? (_AddToQueueCommand = new GenericCommand<ContentItemBase>("AddToQueueCommand", async (m) => await this.AddToQueueAsync(m), this.CanAddToQueue)); }
        }

        private CommandBase _RemoveFromQueueCommand = null;
        public CommandBase RemoveFromQueueCommand
        {
            get { return _RemoveFromQueueCommand ?? (_RemoveFromQueueCommand = new GenericCommand<ContentItemBase>("RemoveFromQueueCommand", async (m) => await this.RemoveFromQueueAsync(m), this.CanRemoveFromQueue)); }
        }

        private NotifyTaskCompletion<IEnumerable<ContentItemBase>> _RecommendedItemsTask;
        public NotifyTaskCompletion<IEnumerable<ContentItemBase>> RecommendedItemsTask
        {
            get { return _RecommendedItemsTask; }
            private set { this.SetProperty(ref _RecommendedItemsTask, value); }
        }

        private NotifyTaskCompletion<IEnumerable<ContentItemBase>> _FriendsWatchedItemsTask;
        public NotifyTaskCompletion<IEnumerable<ContentItemBase>> FriendsWatchedItemsTask
        {
            get { return _FriendsWatchedItemsTask; }
            private set { this.SetProperty(ref _FriendsWatchedItemsTask, value); }
        }

        #endregion Properties

        #region Constructors

        public QueueViewModel()
        {
            this.Title = "Queue";

            if (DesignMode.DesignModeEnabled)
                return;

            this.IsRefreshVisible = true;
            this.QueuePreview = new QueueCollection();
            this.Queue = new QueueCollection();
        }

        #endregion Constructors

        #region Methods

        protected override async Task OnLoadStateAsync(LoadStateEventArgs e, bool isFirstRun)
        {
            if (isFirstRun)
            {
                await this.RefreshAsync();
            }

            await base.OnLoadStateAsync(e, isFirstRun);
        }

        protected override async Task OnRefreshAsync(CancellationToken ct)
        {
            try
            {
                this.ShowBusyStatus(Strings.Resources.TextLoading, true);

                this.RecommendedItemsTask = new NotifyTaskCompletion<IEnumerable<ContentItemBase>>(DataSource.Current.GetRecommendedItemsAsync(ct));
                this.FriendsWatchedItemsTask = new NotifyTaskCompletion<IEnumerable<ContentItemBase>>(DataSource.Current.GetFriendsWatchedItemsAsync(ct));
                
                // Load queue data
                var list = new QueueCollection();
                list.AddRange(await DataSource.Current.GetQueueItemsAsync(Platform.Current.AuthManager.User?.ID, ct));
                this.Queue = list;

                ct.ThrowIfCancellationRequested();

                this.ShowBusyStatus("Updating tiles...");
                await Platform.Current.Notifications.CreateOrUpdateTileAsync(this);

                this.ClearStatus();
            }
            catch (OperationCanceledException)
            {
                this.ShowTimedStatus(Strings.Resources.TextCancellationRequested, 3000);
            }
            catch (Exception ex)
            {
                this.ShowTimedStatus(Strings.Resources.TextErrorGeneric);
                Platform.Current.Logger.LogError(ex, "Error during RefreshAsync");
            }
        }

        protected override Task OnSaveStateAsync(SaveStateEventArgs e)
        {
            return base.OnSaveStateAsync(e);
        }

        private void UpdateQueueProperties()
        {
            if (this.Queue != null)
            {
                this.QueuePreview = new QueueCollection();
                this.QueuePreview.AddRange(this.Queue.Take(3));
                this.NextQueueItem = this.Queue.FirstOrDefault();
                this.ResumeItem = this.Queue.LastOrDefault(); // TODO pull from datasource
            }
            else
            {
                this.QueuePreview = null;
                this.NextQueueItem = null;
            }
        }

        private bool CanAddToQueue(ContentItemBase item)
        {
            return !this.Queue.ContainsItem(item);
        }

        private bool CanRemoveFromQueue(ContentItemBase item)
        {
            return this.Queue.ContainsItem(item);
        }

        private async Task AddToQueueAsync(ContentItemBase item)
        {
            try
            {
                await DataSource.Current.AddToQueueAsync(Platform.Current.AuthManager.User?.ID, item, CancellationToken.None);
                if(!this.Queue.ContainsItem(item))
                    this.Queue.Insert(0, new QueueModel() { Item = item });
            }
            catch(Exception ex)
            {
                Platform.Current.Logger.LogError(ex, "Error during AddToQueueAsync");
            }
            finally
            {
                this.AddToQueueCommand.RaiseCanExecuteChanged();
                this.RemoveFromQueueCommand.RaiseCanExecuteChanged();
            }
        }

        private async Task RemoveFromQueueAsync(ContentItemBase item)
        {
            try
            {
                await DataSource.Current.RemoveFromQueueAsync(Platform.Current.AuthManager.User?.ID, item, CancellationToken.None);
                this.Queue.RemoveItem(item);
            }
            catch (Exception ex)
            {
                Platform.Current.Logger.LogError(ex, "Error during AddToQueueAsync");
            }
            finally
            {
                this.AddToQueueCommand.RaiseCanExecuteChanged();
                this.RemoveFromQueueCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion Methods
    }

    public partial class QueueViewModel
    {
        /// <summary>
        /// Self-reference back to this ViewModel. Used for designtime datacontext on pages to reference itself with the same "ViewModel" accessor used 
        /// by x:Bind and it's ViewModel property accessor on the View class. This allows you to do find-replace on views for 'Binding' to 'x:Bind'.
        [Newtonsoft.Json.JsonIgnore()]
        [System.Runtime.Serialization.IgnoreDataMember()]
        public QueueViewModel ViewModel { get { return this; } }
    }
}

namespace MediaAppSample.Core.ViewModels.Designer
{
    public sealed class QueueViewModel : MediaAppSample.Core.ViewModels.QueueViewModel
    {
        public QueueViewModel()
        {
        }
    }
}