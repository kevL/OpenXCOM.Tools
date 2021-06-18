﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

using DSShared;

using MapView.Forms.Observers;

using XCom;


namespace MapView.Forms.MainView
{
	internal static class ObserverManager
	{
		#region Fields (static)
		internal static ToolstripFactory ToolFactory;

		/// <summary>
		/// An array of controls that will act as observers.
		/// </summary>
		private static IObserver[] _observers;

		/// <summary>
		/// A list of forms that shall be closed when MapView quits.
		/// </summary>
		private static readonly IList<Form> _viewers = new List<Form>();
		#endregion Fields (static)


		#region Properties (static)
		internal static TileViewForm TileView
		{ get; private set; }

		internal static TopViewForm TopView
		{ get; private set; }

		internal static RouteViewForm RouteView
		{ get; private set; }

		internal static TopRouteViewForm TopRouteView
		{ get; private set; }
		#endregion Properties (static)


		#region Methods (static)
		/// <summary>
		/// Creates the observers (secondary viewers) and adds
		/// toolstrip-controls for <c><see cref="MainViewF"/></c> and
		/// <c><see cref="Observers.TopView"/></c> (<c>TopView</c> and
		/// <c>TopRouteView</c>). Also synchronizes Optionables for
		/// <c>TopRouteView(Top)</c> and <c>TopRouteView(Route)</c> to
		/// <c><see cref="Observers.TopView.Optionables">TopView.Optionables</see></c>
		/// and <c><see cref="Observers.RouteView.Optionables">RouteView.Optionables</see></c>
		/// respectively. Then loads default options for <c><see cref="Observers.TileView"/></c>,
		/// <c><see cref="Observers.TopView"/></c>, and <c><see cref="Observers.RouteView"/></c>.
		/// </summary>
		internal static void CreateViewers()
		{
			TileView     = new TileViewForm();
			TopView      = new TopViewForm();
			RouteView    = new RouteViewForm();
			TopRouteView = new TopRouteViewForm();

			ToolFactory  = new ToolstripFactory();

			TopView     .Control   .AddToolstripControls();
			TopRouteView.ControlTop.AddToolstripControls();

			_observers = new IObserver[]
			{
				TileView    .Control,
				TopView     .Control,
				RouteView   .Control,
				TopRouteView.ControlTop,
				TopRouteView.ControlRoute
			};


			// Register the subsidiary viewers via this ObserverManager.
			// TODO: Make TopView's and RouteView's Options static.
			TopRouteView.ControlTop  .Options = ObserverManager.TopView  .Control.Options;
			TopRouteView.ControlRoute.Options = ObserverManager.RouteView.Control.Options;

			LoadDefaultOptions(RegistryInfo.TileView,  TileView);
			LoadDefaultOptions(RegistryInfo.TopView,   TopView);
			LoadDefaultOptions(RegistryInfo.RouteView, RouteView);

			_viewers.Add(TopRouteView);
		}

		/// <summary>
		/// Sets a viewer as an observer and loads its default options.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="f"></param>
		/// <remarks><see cref="TileViewForm"/>, <see cref="TopViewForm"/>,
		/// <see cref="RouteViewForm"/> only.</remarks>
		private static void LoadDefaultOptions(string key, IObserverProvider f)
		{
			//DSShared.LogFile.WriteLine("ObserverManager.LoadDefaultOptions()");
			//DSShared.LogFile.WriteLine(". key= " + key);
			//DSShared.LogFile.WriteLine(". f= " + (f as Form).Name);

			_viewers.Add(f as Form);

			ObserverControl observer = f.Observer; // ie. TileView, TopView, RouteView.
			observer.LoadControlDefaultOptions();
			OptionsManager.setOptionsType(key, observer.Options);
		}

		/// <summary>
		/// Sets or resets the MapFile for each observer-control.
		/// </summary>
		/// <param name="file"></param>
		internal static void SetMapfile(MapFile file)
		{
			foreach (var observer in _observers)
				SubscribeObserver(file, observer);

			MainViewOverlay.that.Refresh();
		}

		/// <summary>
		/// Subscribes <c><see cref="IObserver.OnLocationSelectedObserver"/></c>
		/// and <c><see cref="IObserver.OnLevelSelectedObserver"/></c> handlers
		/// to a specified <c><see cref="MapFile">MapFile.LocationSelected</see></c>
		/// and <c><see cref="MapFile">MapFile.LevelSelected</see></c> event for
		/// an <c><see cref="IObserver"/></c> incl/
		/// <list type="bullet">
		/// <item><c><see cref="Observers.TileView"/></c></item>
		/// <item><c><see cref="Observers.TopView"/></c></item>
		/// <item><c><see cref="Observers.RouteView"/></c></item>
		/// <item><c><see cref="Observers.TopControl"/></c> in <c>TopView</c></item>
		/// <item><c><see cref="Observers.QuadrantControl"/></c> in <c>TopView</c></item>
		/// </list>
		/// </summary>
		/// <param name="file"></param>
		/// <param name="observer"></param>
		private static void SubscribeObserver(MapFile file, IObserver observer)
		{
			// TODO: This stuff is so useless/redundant (think TileView eg.)
			// that the observer hierarchy needs to be reworked from scratch -
			// ie. precision calls are warranted.

			if (observer.MapFile != null)
			{
				observer.MapFile.LocationSelected -= observer.OnLocationSelectedObserver;
				observer.MapFile.LevelSelected    -= observer.OnLevelSelectedObserver;
			}

			if ((observer.MapFile = file) != null)
			{
				observer.MapFile.LocationSelected += observer.OnLocationSelectedObserver;
				observer.MapFile.LevelSelected    += observer.OnLevelSelectedObserver;
			}

			foreach (string key in observer.ObserverControls.Keys)						// ie. TopControl and QuadrantControl
				SubscribeObserver(observer.MapFile, observer.ObserverControls[key]);	// ie. This recursion is req'd only for TopView.
		}

		/// <summary>
		/// Closes each Observers' parent-Form.
		/// <list>
		/// <item><c><see cref="TileViewForm"/></c></item>
		/// <item><c><see cref="TopViewForm"/></c></item>
		/// <item><c><see cref="RouteViewForm"/></c></item>
		/// <item><c><see cref="TopRouteViewForm"/></c></item>
		/// </list>
		/// </summary>
		/// <remarks>Called by <c>MainViewF.SafeQuit()</c> so this really does
		/// close the forms and update registry values.</remarks>
		internal static void CloseViewers()
		{
			foreach (var f in _viewers)
				f.Close();
		}
		#endregion Methods (static)


		#region Update UI (static)
		/// <summary>
		/// Invalidates the <see cref="TopControl">TopControls</see> in TopView
		/// and TopRouteView(Top).
		/// </summary>
		internal static void InvalidateTopControls()
		{
			TopView     .Control   .TopControl.Invalidate();
			TopRouteView.ControlTop.TopControl.Invalidate();
		}

		/// <summary>
		/// Invalidates the <see cref="QuadrantControl">QuadrantControls</see>
		/// in TopView and TopRouteView(Top).
		/// </summary>
		internal static void InvalidateQuadrantControls()
		{
			TopView     .Control   .QuadrantControl.Invalidate();
			TopRouteView.ControlTop.QuadrantControl.Invalidate();
		}
		#endregion Update UI (static)
	}
}
