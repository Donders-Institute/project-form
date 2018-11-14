"use strict";

jQuery(function($) {
    $("#delete-proposal-modal").on("show.bs.modal", function(event) {
        var $button = $(event.relatedTarget);
        var id = $button.data("proposal-id");

        var $form = $(this).find("form");
        $form.prop("action", $form.data("url").replace(/__ID__/g, encodeURIComponent(id)));
    });
});
