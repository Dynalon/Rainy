/*global $:false */
/*global angular:false */

/*global backend:true*/
/*global backend_url:true*/
/*global admin_pw:true*/

var backend_url = "http://localhost:8080/";
var admin_pw = "admin";

var backend = {
    ajax: function(rel_url, options) {
        if (options === undefined)
            options = {};
        var abs_url = backend_url + rel_url;
        options.beforeSend = function(request) {
            request.setRequestHeader("Authority", admin_pw);
        };
        var ret = $.ajax(abs_url, options);

        ret.fail(function(jqxhr, textStatus) {
            if (jqxhr.status == 401) {
                //$('#loginModal').modal();
            }
        });
        return ret;
    }
};
