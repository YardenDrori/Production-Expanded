using System;
using System.Globalization;
using System.Xml;
using Verse;

namespace ProcessorFramework;

public class BonusOutput
{
	public ThingDef thingDef;

	public float chance;

	public int amount;

	public void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		if (xmlRoot.ChildNodes.Count != 1)
		{
			Log.Error("PF: RandomProductList configured incorrectly");
			return;
		}
		string[] array = xmlRoot.FirstChild.Value.TrimStart(new char[1] { '(' }).TrimEnd(new char[1] { ')' }).Split(new char[1] { ',' });
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		chance = Convert.ToSingle(array[0], invariantCulture);
		amount = Convert.ToInt32(array[1], invariantCulture);
		DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef((object)this, "thingDef", xmlRoot.Name, (string)null, (string)null, (Type)null);
	}
}
