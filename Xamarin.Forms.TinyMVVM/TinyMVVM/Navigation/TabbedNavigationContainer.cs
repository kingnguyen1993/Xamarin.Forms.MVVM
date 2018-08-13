﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xamarin.Forms.TinyMVVM
{
    public class TabbedNavigationContainer : TabbedPage, INavigationService
    {
        private List<Page> _tabs = new List<Page>();
        public IEnumerable<Page> TabbedPages { get => _tabs; }

        public TabbedNavigationContainer() : this(Constants.DefaultNavigationServiceName)
        {
        }

        public TabbedNavigationContainer(string navigationServiceName)
        {
            NavigationServiceName = navigationServiceName;
            RegisterNavigation();
        }

        protected void RegisterNavigation()
        {
            TinyIOC.Container.Register<INavigationService>(this, NavigationServiceName);
        }

        public virtual Page AddTab<T>(string title, string icon, object data = null) where T : TinyViewModel
        {
            var page = ViewModelResolver.ResolveViewModel<T>(data);
            return AddTab(page, title, icon);
        }

        public virtual Page AddTab(string modelName, string title, string icon, object data = null)
        {
            var pageModelType = Type.GetType(modelName);
            return AddTab(pageModelType, title, icon, data);
        }

        public virtual Page AddTab(Type pageType, string title, string icon, object data = null)
        {
            var page = ViewModelResolver.ResolveViewModel(pageType, data);
            return AddTab(page, title, icon);
        }

        private Page AddTab(Page page, string title, string icon)
        {
            var viewModel = page.GetModel();
            viewModel.CurrentNavigationServiceName = NavigationServiceName;
            _tabs.Add(page);
            var navigationContainer = CreateContainerPageSafe(page);
            navigationContainer.Title = title;
            if (!string.IsNullOrWhiteSpace(icon))
                navigationContainer.Icon = icon;
            Children.Add(navigationContainer);
            viewModel.OnPushed();

            return navigationContainer;
        }

        internal Page CreateContainerPageSafe(Page page)
        {
            if (page is NavigationPage || page is MasterDetailPage || page is TabbedPage)
                return page;

            return CreateContainerPage(page);
        }

        protected virtual Page CreateContainerPage(Page page)
        {
            return new NavigationPage(page);
        }

        public Task PushPage(Page page, TinyViewModel model, bool modal = false, bool animate = true)
        {
            if (modal)
                return CurrentPage.Navigation.PushModalAsync(CreateContainerPageSafe(page));
            return CurrentPage.Navigation.PushAsync(page);
        }

        public Task PushPage(Page page, bool modal = false, bool animate = true)
        {
            if (modal)
                return CurrentPage.Navigation.PushModalAsync(CreateContainerPageSafe(page));
            return CurrentPage.Navigation.PushAsync(page);
        }

        public Task PopPage(bool modal = false, bool animate = true)
        {
            if (modal)
                return CurrentPage.Navigation.PopModalAsync(animate);
            return CurrentPage.Navigation.PopAsync(animate);
        }

        public Task PopToRoot(bool animate = true)
        {
            return CurrentPage.Navigation.PopToRootAsync(animate);
        }

        public string NavigationServiceName { get; private set; }

        public void NotifyChildrenPageWasPopped()
        {
            foreach (var page in this.Children)
            {
                if (page is NavigationPage)
                    ((NavigationPage)page).NotifyAllChildrenPopped();
            }
        }

        public Task<TinyViewModel> SwitchSelectedRootViewModel<T>() where T : TinyViewModel
        {
            var page = _tabs.FindIndex(o => o.GetModel().GetType().FullName == typeof(T).FullName);

            if (page > -1)
            {
                CurrentPage = this.Children[page];
                var topOfStack = CurrentPage.Navigation.NavigationStack.LastOrDefault();
                if (topOfStack != null)
                    return Task.FromResult(topOfStack.GetModel());
            }
            return null;
        }
    }
}