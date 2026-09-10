using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Util
{
	public class ElementBuilder
	{
		private XmlDocument doc;
		private XmlNode el;

		public ElementBuilder()
		{
			this.doc = new XmlDocument();
			this.el = null;
		}

		private ElementBuilder(XmlDocument doc, XmlNode el)
		{
			this.doc = doc;
			this.el = el;
		}

		public XmlNode Element
		{
			get
			{
				return el;
			}
		}

		public XmlNode Node
		{
			get
			{
				if (Element == null) return doc;
				else return Element;
			}
		}

		public ElementBuilder child(string name)
		{
			ElementBuilder b = new ElementBuilder(Document, Document.CreateElement(name));
			Node.AppendChild(b.Element);
			return b;
		}

		public ElementBuilder comment(string text)
		{
			XmlComment c = Document.CreateComment(text);
			Node.AppendChild(c);
			return this;
		}

		public ElementBuilder attribute(string name, string value)
		{
			XmlAttribute a = Document.CreateAttribute(name);
			a.InnerText = value;
			Element.Attributes.Append(a);
			return this;
		}
		public ElementBuilder attribute(string name, string format, params object[] args)
		{
			return attribute(name, string.Format(format, args));
		}

		public ElementBuilder text(string text)
		{
			XmlText t = Document.CreateTextNode(text);
			Node.AppendChild(t);
			return this;
		}

		public ElementBuilder cdata(string text)
		{
			XmlCDataSection t = Document.CreateCDataSection(text);
			Node.AppendChild(t);
			return this;
		}

		public XmlDocument Document
		{
			get
			{
				return doc;
			}
		}

		public ElementBuilder Parent
		{
			get
			{
				if (Node.ParentNode == null) return new ElementBuilder(doc, null);
				else return new ElementBuilder(doc, (XmlElement)Node.ParentNode);
			}
		}

		public ElementBuilder header()
		{
			if (IsRoot == false) throw new Exception("header requires root, this is not a root");
			Node.AppendChild(doc.CreateNode(XmlNodeType.XmlDeclaration, "", ""));
			return this;
		}
		private ElementBuilder Append(XmlNode node)
		{
			Node.AppendChild(node);
			return new ElementBuilder(doc, node);
		}

		public bool IsRoot
		{
			get
			{
				return (Node == Document);
			}
		}

		public ElementBuilder instruction(string name, string target)
		{
			Append(doc.CreateProcessingInstruction(name, target));
			return this;
		}

		private static string BuildTarget(params KeyValuePair<string, string>[] args)
		{
			return
				new StringListCombiner(" ").combineFromEnumerable(
					new StringList().AddRange<string, string>(args, "{0} = \"{1}\"")
					);
		}
		private static KeyValuePair<string, string> SingleTarget(string key, string val)
		{
			return new KeyValuePair<string, string>(key, val);
		}

		public ElementBuilder xslt(string path)
		{
			if (IsRoot == false) throw new Exception("xslt requires root, this is not a root");
			return instruction("xml-stylesheet", BuildTarget(SingleTarget("type", "text/xsl"), SingleTarget("href", path)));
		}
	}
}
