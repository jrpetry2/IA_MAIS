using Microsoft.Maui.Controls;
using MotionEventActions = Android.Views.MotionEventActions;
using NativeView = Android.Views.View;

namespace SaphMovel.Platforms.Android.Ui;

/// <summary>Liga pressionar/soltar no Android (PointerGestureRecognizer não é fiável).</summary>
static class OuvirPressHoldAttach
{
	const string ViewTag = "saph_ouvir_press_hold";

	public static void Attach(VisualElement element, Action onPress, Action onRelease)
	{
		void OnHandlerChanged(object? sender, EventArgs e)
		{
			if (element.Handler?.PlatformView is not NativeView view)
				return;
			if (view.Tag?.ToString() == ViewTag)
				return;

			view.Tag = ViewTag;
			view.Touch += (_, touchArgs) =>
			{
				var ev = touchArgs.Event;
				if (ev is null)
					return;

				switch (ev.ActionMasked)
				{
					case MotionEventActions.Down:
						view.Parent?.RequestDisallowInterceptTouchEvent(true);
						onPress();
						touchArgs.Handled = true;
						break;
					case MotionEventActions.Up:
					case MotionEventActions.Cancel:
						view.Parent?.RequestDisallowInterceptTouchEvent(false);
						onRelease();
						touchArgs.Handled = true;
						break;
				}
			};
		}

		element.HandlerChanged += OnHandlerChanged;
		if (element.Handler is not null)
			OnHandlerChanged(element, EventArgs.Empty);
	}
}
