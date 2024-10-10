using System;
using System.Data;
                       
[assembly:CLSCompliant(true)] 
namespace ParsElecom.ConfigManager
{	
	public interface IReadOnlyProfile : ICloneable
	{
		string Name
		{
			get; 
		}

		object GetValue(string section, string entry);
		
		string GetValue(string section, string entry, string defaultValue);
		
		int GetValue(string section, string entry, int defaultValue);

		double GetValue(string section, string entry, double defaultValue);

		bool GetValue(string section, string entry, bool defaultValue);

		bool HasEntry(string section, string entry);

		bool HasSection(string section);

		string[] GetEntryNames(string section);

		string[] GetSectionNames();

		DataSet GetDataSet();
	}

	public interface IProfile : IReadOnlyProfile
	{
		new string Name
		{
			get; 
			set;
		}

		string DefaultName
		{
			get;
		}

		bool ReadOnly
		{
			get; 
			set;
		}		
	
		void SetValue(string section, string entry, object value);
		
		void RemoveEntry(string section, string entry);

		void RemoveSection(string section);
		
		void SetDataSet(DataSet ds);
		
		IReadOnlyProfile CloneReadOnly();
		
		event ProfileChangingHandler Changing;

		event ProfileChangedHandler Changed;				
	}
}

