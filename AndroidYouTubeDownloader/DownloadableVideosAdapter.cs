using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AndroidYouTubeDownloader.ViewModels;
using System;
using System.Collections.Generic;

namespace AndroidYouTubeDownloader
{
    internal class DownloadItemsAdapter : RecyclerView.Adapter
    {
        private const int IsHeader = 0;
        private const int IsItem = 1;

        private readonly List<IStreamVM> _streams;

        public DownloadItemsAdapter()
        {
            _streams = new List<IStreamVM>();
        }

        public DownloadItemsAdapter(List<IStreamVM> streams)
        {
            _streams = streams;
        }

        public event EventHandler<int> ItemClick;

        public override int ItemCount => _streams.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            int viewType = GetItemViewType(position);
            var stream = _streams[position];

            switch (viewType)
            {
                case IsHeader:
                    var hvh = holder as HeaderViewHolder;
                    hvh.HeaderTextView.Text = stream.Label;
                    break;
                case IsItem:
                    var ivh = holder as ItemViewHolder;
                    ivh.LabelTextView.Text = stream.Label;
                    break;
                default:
                    throw new ArgumentException("Incorrect viewType");
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var layoutInflater = LayoutInflater.From(parent.Context);
            switch (viewType)
            {
                case IsHeader:
                    var headerView = layoutInflater.Inflate(Resource.Layout.header_layout, parent, false);
                    return new HeaderViewHolder(headerView);
                case IsItem:
                    var itemView = layoutInflater.Inflate(Resource.Layout.download_item, parent, false);
                    return new ItemViewHolder(itemView, OnClick);
                default:
                    throw new ArgumentException("Incorrect viewType");
            }
        }

        public override int GetItemViewType(int position)
        {
            if (_streams[position] is AudioStreamVM || _streams[position] is VideoStreamVM) return IsItem;
            else return IsHeader;
        }

        public void Replace(List<IStreamVM> streams)
        {
            _streams.Clear();
            _streams.AddRange(streams);
            NotifyDataSetChanged();
        }

        public IStreamVM Get(int position) => _streams[position];

        private void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

    }
    public class ItemViewHolder : RecyclerView.ViewHolder
    {
        public TextView LabelTextView { get; private set; }

        public ItemViewHolder(View itemView, Action<int> listener) : base(itemView)
        {
            LabelTextView = itemView.FindViewById<TextView>(Resource.Id.itemLabel);
            itemView.Click += (sender, e) => listener(base.LayoutPosition);
        }
    }

    public class HeaderViewHolder : RecyclerView.ViewHolder
    {
        public TextView HeaderTextView { get; private set; }

        public HeaderViewHolder(View itemView) : base(itemView)
        {
            HeaderTextView = itemView.FindViewById<TextView>(Resource.Id.headerText);
        }
    }
}
