﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using HtmlAgilityPack;

namespace wpXml2Jekyll
{
    public class PostWriter
    {
        private readonly String _postTypeAttachment = "attachment";

        private List<Uri> images = new List<Uri>();

        public int WritePostToMarkdown(XmlDocument xmlDocumentToWrite, string outputFolder)
        {
            var items = xmlDocumentToWrite.SelectNodes("//item");
            int postCount = 0;
            var namespaceManager = new XmlNamespaceManager(xmlDocumentToWrite.NameTable);
            namespaceManager.AddNamespace("wp", "http://wordpress.org/export/1.2/");
            namespaceManager.AddNamespace("content", "http://purl.org/rss/1.0/modules/content/");
            namespaceManager.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
            {
                foreach (XmlNode item in items)
                {
                    
                    String postType = item.SelectSingleNode("wp:post_type", namespaceManager).InnerText;

                    //check if the item is post,page or attachment
                    //attachments shouldn't be saved as a post
                    if (!String.Equals(postType, _postTypeAttachment))
                    {
                        var p = new Post(item.SelectSingleNode("title").InnerText, item.SelectSingleNode("wp:post_date", namespaceManager).InnerText, item.SelectSingleNode("content:encoded", namespaceManager).InnerText,
                             item.SelectSingleNode("wp:post_name", namespaceManager).InnerText, item.SelectSingleNode("dc:creator", namespaceManager).InnerText);

                        var categories = item.SelectNodes("category[@domain='category']", namespaceManager);
                        var tags = item.SelectNodes("category[@domain='post_tag']", namespaceManager);

                        String postStatus = item.SelectSingleNode("wp:status", namespaceManager).InnerText;
                        var folderPath = AppendStatusToOutputFolder(outputFolder, postStatus);
                        CreateDirectoryIfDoesntExist(folderPath);

                        using (
                            TextWriter tw =
                                new StreamWriter(folderPath + Path.DirectorySeparatorChar +
                                                 p.date.ToString("yyyy-MM-dd-") + p.url + ".md"))
                        {
                            tw.WriteLine("---");
                            tw.Write("layout: ");
                            tw.WriteLine(postType);//different layout for pages
                            tw.WriteLine("title: \"" + p.title.Replace("\"", "&quot;") + "\"");
                            tw.WriteLine("date: " + p.date.ToString("yyyy-MM-dd HH:mm"));
                            tw.WriteLine("author: " + p.author);
                            tw.WriteLine("comments: true");
                            tw.Write("categories: [");
                            for(int i = 0; i < categories.Count; i++)
                            {
                                tw.Write(categories[i].InnerText);
                                if (i + 1 < categories.Count)
                                {
                                    tw.Write(", ");
                                }
                            }
                            tw.WriteLine("]");
                            tw.Write("tags: [");
                            for (int i = 0; i < tags.Count; i++)
                            {
                                tw.Write(tags[i].InnerText);
                                if (i + 1 < tags.Count)
                                {
                                    tw.Write(", ");
                                }
                            }
                            tw.WriteLine("]");
                            tw.WriteLine("---");
                            tw.WriteLine(p.content);
                            postCount++;
                        }
                    }
                }
            }
            return postCount;
        }

        private static void CreateDirectoryIfDoesntExist(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        private static string AppendStatusToOutputFolder(string outputFolder, string postStatus)
        {
            String folderPath = outputFolder;
            if (!String.IsNullOrWhiteSpace(postStatus))
            {
                folderPath = outputFolder + Path.DirectorySeparatorChar + postStatus;
            }
            return folderPath;
        }

        public string Process(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            foreach (var img in doc.DocumentNode.Descendants("img").ToList())
            {
                var src = img.GetAttributeValue("src", null);
                var width = img.GetAttributeValue("width", 0);
                var height = img.GetAttributeValue("height", 0);
                if (string.IsNullOrEmpty(src) || width <= 1 || height <= 1)
                {
                    img.Remove();
                    continue;
                }

                Uri imageUri = new Uri(src, UriKind.RelativeOrAbsolute);
                if (!imageUri.IsAbsoluteUri)
                    continue;

                images.Add(imageUri);

                var filename = imageUri.Segments[imageUri.Segments.Length - 1];

                img.SetAttributeValue("src", "{{ site.url }}/images/" + filename);
            }

            return doc.DocumentNode.OuterHtml;
        }
    }
}