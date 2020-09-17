using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;
using Console = System.Console;

[Component(PropertyGuid = "18aa0524bba18e3305467df6e4395cddbfedb898")]
public class Test1BasicComponent : BasicComponent
{
	[ShowInEditor] public int Integer = 0;

	[ShowInEditor] public float Floater = 0.0F;

	[ShowInEditor] public float Floater2 = 0.0F;
	
	[ShowInEditor] public float Floater3 = 0.0F; 

	[ShowInEditor] public string Stringer = "";

	[ShowInEditor] public vec3 Position;

	private void Init()
	{
		// write here code to be called on component initialization
		Startup();
		Visualizer.Enabled = true;
		
	}
	
	private void Update()
	{
		// write here code to be called before updating each render frame
		// MathLib.Lerp(Floater, Floater + 5, );
		Visualizer.RenderSphere(Floater2, node.Transform, new vec4(Integer, Integer, 0, 1.0F));
		if (Floater2 < Floater)
		{
			Floater2 = Floater + 2;
		}
		else
		{
			Floater2 = Floater - 2;
		}

		base.Update();
	}
}