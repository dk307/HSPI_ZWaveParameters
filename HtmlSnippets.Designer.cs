﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Hspi {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class HtmlSnippets {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal HtmlSnippets() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Hspi.HtmlSnippets", typeof(HtmlSnippets).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;script&gt;
        ///function refreshAllZWaveParameters(idParametersGroup) {
        ///	$(&apos;#&apos; + idParametersGroup).find(&apos;button.btn.btn-secondary.refresh-z-wave&apos;).each(function() {		
        ///		$(this).click();
        ///	});
        ///}
        ///&lt;/script&gt;.
        /// </summary>
        internal static string AllParametersScript {
            get {
                return ResourceManager.GetString("AllParametersScript", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;script&gt;
        ///$(&apos;#{0}&apos;).ready(function() {{
        ///    $(&apos;#{1}&apos;).click();
        ///}});
        ///&lt;/script&gt;.
        /// </summary>
        internal static string ClickRefreshButtonScript {
            get {
                return ResourceManager.GetString("ClickRefreshButtonScript", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;script&gt;
        ///function refreshZWaveParameter(homeId, nodeId, parameter, idMessage, idValueWrapper, idValue) {
        ///	var formObject = {
        ///		   homeId: homeId,
        ///		   nodeId: nodeId,
        ///		   parameter: parameter,
        ///		   operation: &quot;GET&quot;
        ///	   };
        ///	   
        ///	$(&apos;#&apos; + idValueWrapper).hide();
        ///	$(&apos;#&apos; + idMessage).html(&apos;&apos;)
        ///	$(&apos;#&apos; + idMessage).addClass(&quot;spinner-border&quot;);
        ///	$(&apos;#&apos; + idMessage).show();	
        ///	
        ///	$.ajax({
        ///		type: &quot;POST&quot;,
        ///		async: &quot;true&quot;,
        ///		url: &apos;/ZWaveParameters/Update&apos;,
        ///		cache: false,
        ///		data: JSON.stringify(formObje [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string PostForRefreshScript {
            get {
                return ResourceManager.GetString("PostForRefreshScript", resourceCulture);
            }
        }
    }
}
