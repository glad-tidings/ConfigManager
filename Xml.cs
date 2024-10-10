using System.IO;
using System.Xml;

namespace ParsElecom.ConfigManager
{
	public class Xml : XmlBased
	{
		private string m_rootName = "profile";

		public Xml()
		{
		}

		public Xml(string fileName) :
			base(fileName)
		{
		}

		public Xml(Xml xml) :
			base(xml)
		{
			m_rootName = xml.m_rootName;
		}

		public override string DefaultName
		{
			get
			{
				return DefaultNameWithoutExtension + ".xml";
			}
		}

		public override object Clone()
		{
			return new Xml(this);
		}

		private string GetSectionsPath(string section)
		{
			return "section[@name=\"" + section + "\"]";
		}
		                              
		private string GetEntryPath(string entry)
		{
			return "entry[@name=\"" + entry + "\"]";
		}

		public string RootName
		{
			get 
			{ 
				return m_rootName; 
			}
			set 
			{ 
				VerifyNotReadOnly();
				if (m_rootName == value.Trim())
					return;
					
				if (!RaiseChangeEvent(true, ProfileChangeType.Other, null, "RootName", value))
					return;

				m_rootName = value.Trim(); 				
				RaiseChangeEvent(false, ProfileChangeType.Other, null, "RootName", value);				
			}
		}

		public override void SetValue(string section, string entry, object value)
		{
			if (value == null)
			{
				RemoveEntry(section, entry);
				return;
			}
			
			VerifyNotReadOnly();
			VerifyName();
			VerifyAndAdjustSection(ref section);
			VerifyAndAdjustEntry(ref entry);

			if (!RaiseChangeEvent(true, ProfileChangeType.SetValue, section, entry, value))
				return;

			string valueString = value.ToString();

			if ((m_buffer == null || m_buffer.IsEmpty) && !File.Exists(Name))
			{	
				XmlTextWriter writer = null;
				
				if (m_buffer == null)
					writer = new XmlTextWriter(Name, Encoding);			
				else
					writer = new XmlTextWriter(new MemoryStream(), Encoding);			

				writer.Formatting = Formatting.Indented;
	            
	            writer.WriteStartDocument();
				
	            writer.WriteStartElement(m_rootName);			
					writer.WriteStartElement("section");
					writer.WriteAttributeString("name", null, section);				
						writer.WriteStartElement("entry");
						writer.WriteAttributeString("name", null, entry);				
	            			writer.WriteString(valueString);
	            		writer.WriteEndElement();
	            	writer.WriteEndElement();
	            writer.WriteEndElement();

				if (m_buffer != null)
					m_buffer.Load(writer);
				writer.Close();            							

				RaiseChangeEvent(false, ProfileChangeType.SetValue, section, entry, value);
				return;
			}
			
			
			XmlDocument doc = GetXmlDocument();
			XmlElement root = doc.DocumentElement;
			
			XmlNode sectionNode = root.SelectSingleNode(GetSectionsPath(section));
			if (sectionNode == null)
			{
				XmlElement element = doc.CreateElement("section");
				XmlAttribute attribute = doc.CreateAttribute("name");
				attribute.Value = section;
				element.Attributes.Append(attribute);			
				sectionNode = root.AppendChild(element);			
			}

			XmlNode entryNode = sectionNode.SelectSingleNode(GetEntryPath(entry));
			if (entryNode == null)
			{
				XmlElement element = doc.CreateElement("entry");
				XmlAttribute attribute = doc.CreateAttribute("name");
				attribute.Value = entry;
				element.Attributes.Append(attribute);			
				entryNode = sectionNode.AppendChild(element);			
			}

			entryNode.InnerText = valueString;
			Save(doc);		
			RaiseChangeEvent(false, ProfileChangeType.SetValue, section, entry, value);
		}

		public override object GetValue(string section, string entry)
		{
			VerifyAndAdjustSection(ref section);
			VerifyAndAdjustEntry(ref entry);
			
			try
			{ 	
				XmlDocument doc = GetXmlDocument();
				XmlElement root = doc.DocumentElement;
				
				XmlNode entryNode = root.SelectSingleNode(GetSectionsPath(section) + "/" + GetEntryPath(entry));
				return entryNode.InnerText;
			}
			catch
			{	
				return null;
			}			
		}

		public override void RemoveEntry(string section, string entry)
		{
			VerifyNotReadOnly();
			VerifyAndAdjustSection(ref section);
			VerifyAndAdjustEntry(ref entry);

			XmlDocument doc = GetXmlDocument();
			if (doc == null)
				return;

			XmlElement root = doc.DocumentElement;			
			XmlNode entryNode = root.SelectSingleNode(GetSectionsPath(section) + "/" + GetEntryPath(entry));
			if (entryNode == null)
				return;

			if (!RaiseChangeEvent(true, ProfileChangeType.RemoveEntry, section, entry, null))
				return;
			
			entryNode.ParentNode.RemoveChild(entryNode);			
			Save(doc);
			RaiseChangeEvent(false, ProfileChangeType.RemoveEntry, section, entry, null);
		}

		public override void RemoveSection(string section)
		{
			VerifyNotReadOnly();
			VerifyAndAdjustSection(ref section);

			XmlDocument doc = GetXmlDocument();
			if (doc == null)
				return;
			
			XmlElement root = doc.DocumentElement;
			if (root == null)
				return;

			XmlNode sectionNode = root.SelectSingleNode(GetSectionsPath(section));
			if (sectionNode == null)
				return;
			
			if (!RaiseChangeEvent(true, ProfileChangeType.RemoveSection, section, null, null))
				return;

			root.RemoveChild(sectionNode);
			Save(doc);
			RaiseChangeEvent(false, ProfileChangeType.RemoveSection, section, null, null);
		}

		public override string[] GetEntryNames(string section)
		{
			if (!HasSection(section))
				return null;
			    			
			VerifyAndAdjustSection(ref section);
			
			XmlDocument doc = GetXmlDocument();
			XmlElement root = doc.DocumentElement;
			
			XmlNodeList entryNodes = root.SelectNodes(GetSectionsPath(section) + "/entry[@name]");
			if (entryNodes == null)
				return null;

			string[] entries = new string[entryNodes.Count];
			int i = 0;

			foreach (XmlNode node in entryNodes)
				entries[i++] = node.Attributes["name"].Value;
			
			return entries;
		}
		
		public override string[] GetSectionNames()
		{
			XmlDocument doc = GetXmlDocument();
			if (doc == null)
				return null;

			XmlElement root = doc.DocumentElement;
			if (root == null)
				return null;

			XmlNodeList sectionNodes = root.SelectNodes("section[@name]");
			if (sectionNodes == null)
				return null;

			string[] sections = new string[sectionNodes.Count];			
			int i = 0;

			foreach (XmlNode node in sectionNodes)
				sections[i++] = node.Attributes["name"].Value;
			
			return sections;
		}		
	}
}
