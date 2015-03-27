<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CreatePost.ascx.cs" Inherits="uBlogsy.Web.usercontrols.uBlogsy.dashboard.CreatePost" %>
<div id="uBlogsy_create_post">

    <h1>Create a post</h1>

    <label for="<%=ddlRoots.ClientID %>">Select site root</label>
    <asp:DropDownList ID="ddlRoots" runat="server"></asp:DropDownList>
    <br />
    <%--<br />
<asp:CheckBox ID="chkbxUseMonthFolder" runat="server" /> <label for="<%=chkbxUseMonthFolder.ClientID %>">Use month folder?</label>
    --%>
    <br />
    <label for="<%=TxtTitle.ClientID %>">Title</label>
    <asp:TextBox runat="server" ID="TxtTitle" placeholder="Blog post/article title" Style="width: 200px;"></asp:TextBox>
    <br />
    <asp:LinkButton ID="btnSubmit" runat="server" OnClick="btnSubmit_Click" Text="Click to create" ValidationGroup="uBlogsyCreatePost" />

    <div id="uBlogsy_loading" style="display: none;">
        <span>Preparing new blog post... </span><span id="uBlogsyCount" style="padding-right: 10px;"></span>
        <img src="/umbraco_client/Tree/Themes/umbraco/throbber.gif" />
    </div>

    <input type="hidden" id="HfRedirectUrl" runat="server"/>
</div>


<script>
    $(document).ready(function () {
        var uBlogsySeconds = 6;
        var uBlogsyIntervalId;

        var redirect = $('input[id$=HfRedirectUrl]').val();
        if (redirect.length > 0) {
            top.location = redirect;
        }


        $('#uBlogsy_create_post a[id$=btnSubmit]').click(function () {
            $(this).hide();
            $('#uBlogsy_loading').show();
            uBlogsyIntervalId = window.setInterval(uBlogsyCountdown, 1000);
            uBlogsyCountdown();
        });

        function uBlogsyCountdown() {
            uBlogsySeconds--;
            $("#uBlogsyCount").text(uBlogsySeconds);
            if (uBlogsySeconds == 0) {
                window.clearInterval(uBlogsyIntervalId);
            }
        }
    });
</script>
