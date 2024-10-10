using System;
using System.Data;
                       
namespace ParsElecom.ConfigManager
{		
	public abstract class Profile : IProfile
	{
		private string m_name;
		private bool m_readOnly;
		
		public event ProfileChangingHandler Changing;

		public event ProfileChangedHandler Changed;				
		
		protected Profile()
		{			
			m_name = DefaultName;
		}
		
		protected Profile(string name)
		{			
			m_name = name;
		}
		
		protected Profile(Profile profile)
		{			
			m_name = profile.m_name;
			m_readOnly = profile.m_readOnly;			
			Changing = profile.Changing;
			Changed = profile.Changed;
		}
		
		public string Name
		{
			get 
			{ 
				return m_name; 
			}
			set 
			{ 
				VerifyNotReadOnly();	
				if (m_name == value.Trim())
					return;
					
				if (!RaiseChangeEvent(true, ProfileChangeType.Name, null, null, value))
					return;
							
				m_name = value.Trim();
				RaiseChangeEvent(false, ProfileChangeType.Name, null, null, value);
			}
		}

		public bool ReadOnly
		{
			get 
			{ 
				return m_readOnly; 
			}
			
			set
			{ 
				VerifyNotReadOnly();
				if (m_readOnly == value)
					return;
				
				if (!RaiseChangeEvent(true, ProfileChangeType.ReadOnly, null, null, value))
					return;
							
				m_readOnly = value;
				RaiseChangeEvent(false, ProfileChangeType.ReadOnly, null, null, value);
			}
		}

		public abstract string DefaultName
		{
			get;
		}

		public abstract object Clone();

		public abstract void SetValue(string section, string entry, object value);
		
		public abstract object GetValue(string section, string entry);

		public virtual string GetValue(string section, string entry, string defaultValue)
		{
			object value = GetValue(section, entry);
			return (value == null ? defaultValue : value.ToString());
		}

		public virtual int GetValue(string section, string entry, int defaultValue)
		{
			object value = GetValue(section, entry);
			if (value == null)
				return defaultValue;

			try
			{
				return Convert.ToInt32(value);
			}
			catch 
			{
				return 0;
			}
		}

		public virtual double GetValue(string section, string entry, double defaultValue)
		{
			object value = GetValue(section, entry);
			if (value == null)
				return defaultValue;

			try
			{
				return Convert.ToDouble(value);
			}
			catch 
			{
				return 0;
			}
		}

		public virtual bool GetValue(string section, string entry, bool defaultValue)
		{
			object value = GetValue(section, entry);
			if (value == null)
				return defaultValue;			

			try
			{
				return Convert.ToBoolean(value);
			}
			catch 
			{
				return false;
			}
		}

		public virtual bool HasEntry(string section, string entry)
		{
			string[] entries = GetEntryNames(section);
			
			if (entries == null)
				return false;

			VerifyAndAdjustEntry(ref entry);
			return Array.IndexOf(entries, entry) >= 0;
		}

		public virtual bool HasSection(string section)
		{
			string[] sections = GetSectionNames();

			if (sections == null)
				return false;

			VerifyAndAdjustSection(ref section);
			return Array.IndexOf(sections, section) >= 0;
		}

		public abstract void RemoveEntry(string section, string entry);

		public abstract void RemoveSection(string section);
		
		public abstract string[] GetEntryNames(string section);

		public abstract string[] GetSectionNames();
		
		public virtual IReadOnlyProfile CloneReadOnly()
		{
			Profile profile = (Profile)Clone();
			profile.m_readOnly = true;
			
			return profile;
		}

		public virtual DataSet GetDataSet()
		{
			VerifyName();
			
			string[] sections = GetSectionNames();
			if (sections == null)
				return null;
			
			DataSet ds = new DataSet(Name);
			
			foreach (string section in sections)
			{
				DataTable table = ds.Tables.Add(section);
				
				string[] entries = GetEntryNames(section);
				DataColumn[] columns = new DataColumn[entries.Length];
				object[] values = new object[entries.Length];								

				int i = 0;
				foreach (string entry in entries)
				{
					object value = GetValue(section, entry);
				
					columns[i] = new DataColumn(entry, value.GetType());
					values[i++] = value;
				}
												
				table.Columns.AddRange(columns);
				table.Rows.Add(values);								
			}
			
			return ds;
		}
		
		public virtual void SetDataSet(DataSet ds)
		{
			if (ds == null)
				throw new ArgumentNullException("ds");
			
			foreach (DataTable table in ds.Tables)
			{
				string section = table.TableName;
				DataRowCollection rows = table.Rows;				
				if (rows.Count == 0)
					continue;

				foreach (DataColumn column in table.Columns)
				{
					string entry = column.ColumnName;
					object value = rows[0][column];
					
					SetValue(section, entry, value);
				}
			}
		}

		protected string DefaultNameWithoutExtension
		{
			get
			{
				try
				{
					string file = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
					return file.Substring(0, file.LastIndexOf('.'));
				}
				catch
				{
					return "profile";
				}
			}
		}

		protected virtual void VerifyAndAdjustSection(ref string section)
		{
			if (section == null)
				throw new ArgumentNullException("section");			
			
			section = section.Trim();
		}

		protected virtual void VerifyAndAdjustEntry(ref string entry)
		{
			if (entry == null)
				throw new ArgumentNullException("entry");			

			entry = entry.Trim();
		}
		
		protected internal virtual void VerifyName()
		{
			if (m_name == null || m_name == "")
				throw new InvalidOperationException("نام خالی است.");
		}

		protected internal virtual void VerifyNotReadOnly()
		{
			if (m_readOnly)
				throw new InvalidOperationException("فایل فقط خواندنی است.");			
		}
		
		protected bool RaiseChangeEvent(bool changing, ProfileChangeType changeType, string section, string entry, object value)
		{
			if (changing)
			{
				if (Changing == null)
					return true;

				ProfileChangingArgs e = new ProfileChangingArgs(changeType, section, entry, value);
				OnChanging(e);
				return !e.Cancel;
			}
			
			if (Changed != null)
				OnChanged(new ProfileChangedArgs(changeType, section, entry, value));
			return true;
		}
		                          
		protected virtual void OnChanging(ProfileChangingArgs e)
		{
			if (Changing == null)
				return;

			foreach (ProfileChangingHandler handler in Changing.GetInvocationList())
			{
				handler(this, e);
				
				if (e.Cancel)
					break;
			}
		}

		protected virtual void OnChanged(ProfileChangedArgs e)
		{
			if (Changed != null)
				Changed(this, e);
		}
		
		public virtual void Test(bool cleanup)
		{
			string task = ""; 
			try
			{
				string section = "تست پروفایل";
				
				task = "مقداردهی اولیه پروفایل - پاک کردن بخش '" + section;
				
					RemoveSection(section);
				
				task = "خواندن و شمارش بخش";
				
					string[] sections = GetSectionNames();
					int sectionCount = (sections == null ? 0 : sections.Length);
					bool haveSections = sectionCount > 1;
				
				task = "افزودن آیتم به بخش '" + section;
				
					SetValue(section, "Text entry", "123 abc"); 
					SetValue(section, "Blank entry", ""); 
					SetValue(section, "Null entry", null);  // nothing will be added
					SetValue(section, "  Entry with leading and trailing spaces  ", "The spaces should be trimmed from the entry"); 
					SetValue(section, "Integer entry", 2 * 8 + 1); 
					SetValue(section, "Long entry", 1234567890123456789); 
					SetValue(section, "Double entry", 2 * 8 + 1.95); 
					SetValue(section, "DateTime entry", DateTime.Today); 
					SetValue(section, "Boolean entry", haveSections); 
				
				task = "افزودن آیتم نول به بخش '" + section;

					try
					{
						SetValue(section, null, "123 abc"); 
						throw new Exception("آیتم نول با موفقیت اضافه شد");
					}
					catch (ArgumentNullException)
					{						
					}
						
				task = "مقدار دهی آیتم نول";

					try
					{
						GetValue(null, "Test"); 
						throw new Exception("آیتم نول با موفقیت محتوا دریافت کرد");
					}
					catch (ArgumentNullException)
					{						
					}

				task = "خواندن و شمارش آیتم ها";
				
					int expectedEntries = 8;
					string[] entries = GetEntryNames(section);

				task = "تایید تعداد آیتم ها " + expectedEntries;
				
					if (entries.Length != expectedEntries)
						throw new Exception("تعداد اشتباهی از آیتم ها یافت شد: " + entries.Length);

				task = "بررسی مقادیر ورودی اضافه شده";
								
					string strValue = GetValue(section, "Text entry", "");
					if (strValue != "123 abc")
						throw new Exception("مقدار اشتباه: '" + strValue + "'");
						
					int nValue = GetValue(section, "Text entry", 321);
					if (nValue != 0)
						throw new Exception("مقدار اشتباه: " + nValue);

					strValue = GetValue(section, "Blank entry", "invalid");
					if (strValue != "")
						throw new Exception("مقدار اشتباه: '" + strValue + "'");
				
					object value = GetValue(section, "Blank entry");
					if (value == null)
						throw new Exception("مقدار اشتباه");

					nValue = GetValue(section, "Blank entry", 321);
					if (nValue != 0)
						throw new Exception("مقدار اشتباه: " + nValue);

					bool bValue = GetValue(section, "Blank entry", true);
					if (bValue != false)
						throw new Exception("مقدار اشتباه: " + bValue);

					strValue = GetValue(section, "Null entry", "");
					if (strValue != "")
						throw new Exception("مقدار اشتباه: '" + strValue + "'");
				
					value = GetValue(section, "Null entry");
					if (value != null)
						throw new Exception("مقدار اشتباه: '" + value + "'");

					strValue = GetValue(section, "  Entry with leading and trailing spaces  ", "");
					if (strValue != "The spaces should be trimmed from the entry")
						throw new Exception("مقدار اشتباه: '" + strValue + "'");

					if (!HasEntry(section, "Entry with leading and trailing spaces"))
						throw new Exception("مقدار اشتباه");

					nValue = GetValue(section, "Integer entry", 0);
					if (nValue != 17)
						throw new Exception("مقدار اشتباه: " + nValue);
					
					double dValue = GetValue(section, "Integer entry", 0.0);
					if (dValue != 17)
						throw new Exception("مقدار اشتباه: " + dValue);

					long lValue = Convert.ToInt64(GetValue(section, "Long entry"));
					if (lValue != 1234567890123456789)
						throw new Exception("مقدار اشتباه: " + lValue);
					
					strValue = GetValue(section, "Long entry", "");
					if (strValue != "1234567890123456789")
						throw new Exception("مقدار اشتباه: '" + strValue + "'");

					dValue = GetValue(section, "Double entry", 0.0);
					if (dValue != 17.95)
						throw new Exception("مقدار اشتباه: " + dValue);

					nValue = GetValue(section, "Double entry", 321);
					if (nValue != 0)
						throw new Exception("مقدار اشتباه: " + nValue);
				
					strValue = GetValue(section, "DateTime entry", "");
					if (strValue != DateTime.Today.ToString())
						throw new Exception("مقدار اشتباه: '" + strValue + "'");

					DateTime today = DateTime.Parse(strValue);
					if (today != DateTime.Today)
						throw new Exception("مقدار اشتباه: '" + strValue + "'");
				
					bValue = GetValue(section, "Boolean entry", !haveSections);
					if (bValue != haveSections)
						throw new Exception("مقدار اشتباه: " + bValue);
					
					strValue = GetValue(section, "Boolean entry", "");
					if (strValue != haveSections.ToString())
						throw new Exception("مقدار اشتباه: '" + strValue + "'");

					value = GetValue(section, "Nonexistent entry");
					if (value != null)
						throw new Exception("مقدار اشتباه: '" + value + "'");

					strValue = GetValue(section, "Nonexistent entry", "Some Default");
					if (strValue != "Some Default")
						throw new Exception("مقدار اشتباه: '" + strValue + "'");

				task = "ایجاد یک کپی از آبجکت فقط خواندنی";
				
					IReadOnlyProfile roProfile = CloneReadOnly();
					
					if (!roProfile.HasSection(section))
						throw new Exception("آبجکت فقط خواندنی وجود ندارد");

					dValue = roProfile.GetValue(section, "Double entry", 0.0);
					if (dValue != 17.95)
						throw new Exception("مقدار اشتباه: " + dValue);
				
				task = "بررسی آبجکت فقط خواندنی جهت امکان مقدار دهی";

					try
					{
						((IProfile)roProfile).ReadOnly = false;
						throw new Exception("امکان مقدار دهی آبجکت فقط خواندنی وجود دارد");
					}
					catch (InvalidOperationException)
					{						
					}

					try
					{
						((IProfile)roProfile).SetValue(section, "Entry which should not be written", "This should not happen");
						throw new Exception("امکان مقدار دهی آبجکت فقط خواندنی وجود ندارد");
					}
					catch (InvalidOperationException)
					{						
					}
											       
				if (!cleanup)
					return;
					
				task = "حذف آیتم های اضافه شده";

					RemoveEntry(section, "Text entry"); 
					RemoveEntry(section, "Blank entry"); 
					RemoveEntry(section, "  Entry with leading and trailing spaces  "); 
					RemoveEntry(section, "Integer entry"); 
					RemoveEntry(section, "Long entry"); 
					RemoveEntry(section, "Double entry"); 
					RemoveEntry(section, "DateTime entry"); 
					RemoveEntry(section, "Boolean entry"); 													

				task = "حذف آیتم های ناموجود";

					RemoveEntry(section, "Null entry"); 

				task = "تمامی آیتم ها حذف شدند";

					entries = GetEntryNames(section);
				
					if (entries.Length != 0)
						throw new Exception("تعداد اشتباهی از آیتم ها پیدا شد: " + entries.Length);

				task = "حذف بخش";

					RemoveSection(section);

				task = "بخش حذف شد";

					int sectionCount2 = GetSectionNames().Length;
				
					if (sectionCount != sectionCount2)
						throw new Exception("تعداد اشتباهی از بخش ها پیدا شد: " + sectionCount2);

					entries = GetEntryNames(section);				
				
					if (entries != null)
						throw new Exception("بخش نول حذف نشده");
			}
			catch (Exception ex)
			{
				throw new Exception("تست ناموفق در " + task, ex);
			}
		}
	}	
}
