# ComCat
ComCat is a Resharper plugin that helps with 2 things in C# programming:
* Order *DataMember*s of *DataContract*
* Synchronize method / proprty XML doc comments between interface / base class and implementation.

#Installation
You will need Resharper (8.2.1+ but not 9.0) installed. Then use the Resharper extensions manager to find and install this plugin

#Usage
* To Order *DataMember*s, open the DataContract class file, make sure the caret is on the class name, a context action should appear with name "Reorder DataMember in this class", click it and a pop-up window will help take care of business.
* To Sync comments, open the implementation class file, make sure the caret is either on the class name, or on a XML document comment block (won't work with other comment types) on a inheriting method / property, 2 context actions should apear with names "Take comments from base class" and "Push comments to base class". *Note* this will not work from the interface declaration.

#Thanks to
Matt Ellis for answering a couple of key questions.
