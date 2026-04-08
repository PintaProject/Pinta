using System;
using System.Collections.Generic;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public readonly record struct RulerChanged (bool Full);
public readonly record struct RulerVisibilityChanged (bool NewVisibility);

public sealed class RulerViewModel :
	IObservable<RulerChanged>,
	IObservable<RulerVisibilityChanged>
{
	private readonly LinkedList<IObserver<RulerChanged>> change_observers = new ();
	private readonly LinkedList<IObserver<RulerVisibilityChanged>> visibility_observers = new ();

	private readonly RulerModel model;
	internal RulerViewModel (RulerModel model)
	{
		this.model = model;
	}

	public Gtk.Orientation Orientation
		=> model.Orientation;

	public double Position {
		get => model.Position;
		set {
			if (model.Position == value) return;
			model.Position = value;
			OnRulerChanged (full: true);
		}
	}

	public MetricType Metric {
		get => model.Metric;
		set {
			if (model.Metric == value) return;
			model.Metric = value;
			OnRulerChanged (full: true);
		}
	}

	public NumberRange<double>? SelectionBounds {
		get => model.SelectionBounds;
		set {
			if (model.SelectionBounds == value) return;
			model.SelectionBounds = value;
			OnRulerChanged (full: false);
		}
	}

	public NumberRange<double> RulerRange {
		get => model.RulerRange;
		set {
			if (model.RulerRange == value) return;
			model.RulerRange = value;
			OnRulerChanged (full: true);
		}
	}

	bool visible = false;
	public bool Visible {
		get => visible;
		set {
			if (visible == value) return;
			visible = value;
			OnVisibilityChanged (value);
		}
	}

	private void OnRulerChanged (bool full)
		=> change_observers.NotifyAll (new (full));

	private void OnVisibilityChanged (bool newVisibility)
		=> visibility_observers.NotifyAll (new (newVisibility));

	public IDisposable Subscribe (IObserver<RulerChanged> observer)
		=> change_observers.Subscribe (observer);

	public IDisposable Subscribe (IObserver<RulerVisibilityChanged> observer)
		=> visibility_observers.Subscribe (observer);
}
