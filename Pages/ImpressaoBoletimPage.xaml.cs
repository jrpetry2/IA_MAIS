using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SaphMovel;
using SaphMovel.ViewModels;

namespace SaphMovel.Pages;

public partial class ImpressaoBoletimPage : ContentPage
{
	readonly MainViewModel _mainVm;
	readonly SignatureDrawable _drawable = new();

	public ImpressaoBoletimPage(MainViewModel mainVm)
	{
		_mainVm = mainVm;
		InitializeComponent();
		SignatureView.Drawable = _drawable;
		SignatureView.StartInteraction += OnSignatureStart;
		SignatureView.DragInteraction += OnSignatureDrag;
		SignatureView.EndInteraction += OnSignatureEnd;
		SignatureView.CancelInteraction += OnSignatureCancel;
		BindingContext = new ImpressaoBoletimViewModel(mainVm, this);
	}

	public bool SignatureHasInk => _drawable.HasInk;

	/// <summary>Grava a assinatura no estado partilhado (chamar antes de fechar após sucesso no envio).</summary>
	public void SaveSignatureToForm() =>
		_mainVm.ImpressaoBoletim.AssinaturaStrokesJson = _drawable.ExportStrokesToJson();

	public void ClearSignaturePad()
	{
		_mainVm.ImpressaoBoletim.AssinaturaStrokesJson = null;
		_drawable.Clear();
		SignatureView.Invalidate();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_drawable.LoadStrokesFromJson(_mainVm.ImpressaoBoletim.AssinaturaStrokesJson);
		SignatureView.Invalidate();
	}

	void OnSignatureStart(object? sender, TouchEventArgs e)
	{
		if (!TryPrimaryPoint(e, out var pt))
			return;
		_drawable.BeginStroke(pt);
		SignatureView.Invalidate();
	}

	void OnSignatureDrag(object? sender, TouchEventArgs e)
	{
		if (!TryPrimaryPoint(e, out var pt))
			return;
		_drawable.AddPoint(pt);
		SignatureView.Invalidate();
	}

	void OnSignatureEnd(object? sender, TouchEventArgs e)
	{
		if (TryPrimaryPoint(e, out var pt))
			_drawable.AddPoint(pt);
		_drawable.EndStroke();
		SignatureView.Invalidate();
	}

	void OnSignatureCancel(object? sender, EventArgs e)
	{
		_drawable.EndStroke();
		SignatureView.Invalidate();
	}

	static bool TryPrimaryPoint(TouchEventArgs e, out PointF pt)
	{
		pt = default;
		var touches = e.Touches;
		switch (touches)
		{
			case null:
				return false;
			case PointF[] arr when arr.Length > 0:
				pt = arr[0];
				return true;
			case IReadOnlyList<PointF> list when list.Count > 0:
				pt = list[0];
				return true;
			default:
				return false;
		}
	}
}
