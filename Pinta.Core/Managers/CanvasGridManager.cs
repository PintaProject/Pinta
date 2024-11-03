using System;

namespace Pinta.Core.Managers;


public interface ICanvasGridService
{
	int CellWidth { get; set; }
	int CellHeight { get; set; }

	public event EventHandler? SizeChanged;
}


public sealed class CanvasGridManager : ICanvasGridService
{
	private int cell_width = 1;
	private int cell_height = 1;

	public int CellWidth {
		get => cell_width;
		set {
			cell_width = value;
			SizeChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public int CellHeight {
		get => cell_height;
		set {
			cell_height = value;
			SizeChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public CanvasGridManager ()
	{
	}

	public event EventHandler? SizeChanged;
}
