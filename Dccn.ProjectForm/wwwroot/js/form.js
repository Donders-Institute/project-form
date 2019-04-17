"use strict";

$(function() {
    var initializers = {};

    function registerSubmitHandlers($root) {
        $("[data-save-on]", $root).each(function() {
            var $input = $(this);
            var $section = $input.closest(".form-section");

            $input.on($input.data("save-on"), function() {
                $section.trigger($.Event("submit", { useHandler: true }));
            });
        });
    }

    var formInfo = JSON.parse($("#form-info").html());

    var requestInfo = {
        timestamp: formInfo.timestamp,
        current: null,
        next: null
    };

    function postSectionRequest($section, build, callback) {
        if (requestInfo.current) {
            requestInfo.next = {
                build,
                callback
            };
        } else {
            var options = build({
                method: "POST",
                dataType: "json"
            });

            requestInfo.current = $.ajax(options).done(callback).done(function(result) {
                var timestamp = options.dataType === "json" ? result.timestamp : $(result).data("timestamp");
                console.log("Updated timestamp. Old: " + requestInfo.timestamp + ". New: " + timestamp);
                requestInfo.timestamp = timestamp;
                $(".timestamp").val(timestamp);
                setTimeout(function() {
                    delete requestInfo.current;
                    var next = requestInfo.next;
                    if (next) {
                        delete requestInfo.next;
                        postSectionRequest($section, next.build, next.callback);
                    }
                }, 100);
            });
        }
    }

    function initializeSection($section) {
        var sectionId = $section.prop("id");

        $section.validate();

        // Workaround for jQuery Validate not supporting form, formaction or formmethod attributes.
        $("button[type='submit']", $section).click(function() {
            var $submit = $(this);
            var form = $submit.prop("form");
            var $form = form ? $("#" + form) : $section;
            $form.trigger($.Event("submit", {
                 useHandler: false,
                 action: $submit.prop("formAction"),
                 method: $submit.prop("formMethod")
            }));
            return false;
        });

        $("input", $section).on("keypress", function(event) {
            return event.which !== 13;
        });

        $section.data("submit-handler", function() {
            postSectionRequest($section, function(options) {
                options.url = $section.attr("action");
                options.data = $section.serialize();
                return options;
            }, function(result) {
                Validation.updateErrors(result.errors);
            });
        });

        $(".user-query", $section).each(function() {
            var $autocomplete = $(this);

            $autocomplete.on("keypress", function(event) {
                if (event.which === 13) {
                    $(".user-add[data-usertype='" + $autocomplete.data("usertype") + "']").click();
                    return false;
                }
                return true;
            });

            $autocomplete.on("input", function() {
                //$autocomplete.removeClass("border-info");
                Validation.clearError($autocomplete);
                $autocomplete.removeData("id");
            });

            $autocomplete.on("autocomplete:selected", function(_event, suggestion) {
               // $autocomplete.addClass("border-info");
                $autocomplete.data("id", suggestion.id);
            });

            $autocomplete.autocomplete({
                    minLength: 2,
                    highlight: true,
                    autoselect: true
                }, {
                    source: function(query, callback) {
                        $.getJSON({
                            url: $autocomplete.data("url").replace(/__QUERY__/g, encodeURIComponent(query))
                        }).done(function(data) {
                            callback(data);
                        });
                    },
                    display: function(suggestion) {
                        return $.fn.autocomplete.escapeHighlightedString(suggestion.name);
                    },
                    templates: {
                        suggestion: function(suggestion) {
                            return $("<div></div>")
                                .text(suggestion.name)
                                .append($("<span></span>").addClass("badge badge-info ml-1").text(suggestion.id))
                                .html();
                        },
                        empty: function(params) {
                            return $("<div></div>")
                                .text("No results for '" + params.query + "'.")
                                .html();
                        }
                    },
                    debounce: 100
                }
            );
        });

        $(".radio-panel-input", $section).change(function() {
            var $input = $(this);
            if ($input.prop("checked")) {
                $($input.data("target")).collapse("show");
            }
        });

        $(".radio-panel-input", $section).each(function() {
            var $input = $(this);
            var $collapse = $($input.data("target"));
            $collapse.on("hide.bs.collapse", function() {
                $collapse.prop("disabled", true);
            });
            $collapse.on("show.bs.collapse", function() {
                $collapse.prop("disabled", false);
            });

            if ($input.prop("checked")) {
                $collapse.addClass("show");
            } else {
                $collapse.prop("disabled", true);
            }
        });

        var initializer = initializers[sectionId];
        if (initializer) {
            initializer($section);
        }

        // Workaround for Bootstrap toggle buttons not inheriting fieldset disabled state.
        $("fieldset:disabled [data-toggle='buttons']", $section).each(function() {
            $(this).prop("disabled", true);
        });

        $("[data-toggle='tooltip']", $section).tooltip({
            html: true
        });

        initListPlaceholders($section);
        registerSubmitHandlers($section);
    }

    function initializeUserList(userType, initialValues, itemDefaults, createItem) {
        var $autocomplete = $(".aa-input.user-query[data-usertype='" + userType + "']");
        var $userlist = $(".user-items[data-usertype='" + userType + "']");

        $(".user-add[data-usertype='" + userType + "']").click(function() {
            var id = $autocomplete.data("id");
            if (!id || $userlist.find("[data-id='" + id + "']").length > 0) {
                Validation.setError($autocomplete, "User is already in the list.");
                //$autocomplete.addClass("border-danger");
                return false;
            }

            var $item = createItem(Object.assign({
                id: id,
                name: $autocomplete.autocomplete("val")
            }, itemDefaults));

            $userlist.append($item);
            registerSubmitHandlers($item);

            $autocomplete.autocomplete("val", "");
            $autocomplete.trigger("input");
            return true;
        });

        initialValues.forEach(function(item) {
            $userlist.append(createItem(item));
        });
    }

    initializers.Funding = function() {
        $("#funding-contact-name").each(function() {
            var $autocomplete = $(this);

            $autocomplete.on("autocomplete:selected", function(_event, suggestion) {
                $("#funding-contact-email").val(suggestion.email);
            });

            $autocomplete.autocomplete({
                    minLength: 2,
                    highlight: true,
                    autoselect: true
                },
                {
                    source: function(query, callback) {
                        $.getJSON({
                            url: $autocomplete.data("url").replace(/__QUERY__/g, encodeURIComponent(query))
                        }).done(function(data) {
                            callback(data);
                        });
                    },
                    display: function(suggestion) {
                        return $.fn.autocomplete.escapeHighlightedString(suggestion.name);
                    },
                    templates: {
                        suggestion: function(suggestion) {
                            return $("<div></div>")
                                .text(suggestion.name)
                                .append($("<span></span>", { "class": "badge badge-info ml-1" }).text(suggestion.id))
                                .html();
                        },
                        empty: function(params) {
                            return $("<div></div>")
                                .text("No results for '" + params.query + "'.")
                                .html();
                        }
                    },
                    debounce: 100
                }
            );
        });
    };

    initializers.Ethics = function() {
        $("#ethics-approval").change(function() {
            var $option = $(this.selectedOptions[0]);
            var value = $option.data("value");
            if (value) {
                $("#ethics-approval-custom").val(value).prop("readonly", true);
            } else {
                $("#ethics-approval-custom").val("").prop("readonly", false);
            }
        });
    };

    initializers.Experiment = function($section) {
        function initializeLabItem($item) {
            function updateTotalDuration() {
                var subjects = parseInt($item.find(".lab-item-subjects").val());
                var sessions = parseInt($item.find(".lab-item-sessions").val());
                var duration = parseInt($item.find(".lab-item-duration").val());
                var totalDuration = moment.duration(subjects * sessions * duration, "minutes");
                $item.find(".lab-item-total-duration").text(totalDuration.isValid() ? totalDuration.asHours().toFixed() + " hour(s)" : "");
            }

            $item.find(".remove-lab-item").click(function() {
                var $button = $(this);

                postSectionRequest($section, function(options) {
                    var formData = $section.serializeArray().reduce(function(obj, entry) {
                        if (entry.name === "Timestamp" || entry.name === "__RequestVerificationToken") {
                            obj[entry.name] = entry.value;
                        }
                        return obj;
                    }, {});

                    options.url = $button.data("url");
                    options.data = formData;
                    return options;
                }, function() {
                    $item.remove();
                    updateStandardQuota();
                });
            });

            $item.find(".lab-item-subjects,.lab-item-sessions,.lab-item-duration").change(updateTotalDuration);
            $item.find(".lab-item-subjects,.lab-item-sessions").change(updateStandardQuota);

            updateTotalDuration();
            updateStandardQuota();
        }

        function updateStandardQuota() {
            var $container = $(".lab-items");
            var $items = $container.children(".lab-item");
            var quota = 0;
            var minimumQuota = parseInt($container.data("storage-minimum"));
            $items.each(function() {
                var $item = $(this);

                var subjects = parseInt($item.find(".lab-item-subjects").val()) || 0;
                var sessions = parseInt($item.find(".lab-item-sessions").val()) || 0;
                var storage = parseInt($item.data("storage")) || 0;
                quota += subjects * sessions * storage;
            });
            if (quota < minimumQuota) {
                quota = minimumQuota;
            }
            $(".quota-standard-value").text((quota / 1000).toFixed(1) + " GB");
        }

        $(".add-lab-item").click(function() {
            var $button = $(this);

            postSectionRequest($section, function(options) {
                var formData = $section.serializeArray().reduce(function(obj, entry) {
                    if (entry.name === "Timestamp" || entry.name === "__RequestVerificationToken") {
                        obj[entry.name] = entry.value;
                    }
                    return obj;
                }, {});

                options.url = $button.attr("formaction");
                options.data = formData;
                options.dataType = "html";
                return options;
            }, function(html) {
                var $item = $(html);
                initializeLabItem($item);
                $(".lab-items").append($item);
                registerSubmitHandlers($item);
            });
        });

        $(".lab-item").each(function() {
            initializeLabItem($(this));
        });
        updateStandardQuota();

        var experimenterItemTemplate = $.templates("#experimenter-item-template");
        initializeUserList("experimenter", JSON.parse($("#experimenters").html()), {}, function(data) {
            var $item = $(experimenterItemTemplate.render(data));
            $item.find(".remove-experimenter").click(function() {
                $(this).closest(".experimenter-item").remove();
            });
            return $item;
        });
    };

    initializers.Data = function() {
        var accessItemTemplate = $.templates("#access-item-template");
        initializeUserList("access", JSON.parse($("#access-rules").html()), { role: "Viewer" }, function(data) {
            var $item = $(accessItemTemplate.render(data));
            $item.find(".remove-access").click(function() {
                $(this).closest(".access-item").remove();
            });
            return $item;
        });

        $("#repository-user-warning").each(function() {
            var $alert = $(this);
            $.getJSON({
                url: $alert.data("url")
            }).done(function(json) {
                if (json === false) {
                    $alert.removeClass("d-none");
                }
            });
        });
    };

    initializers.Privacy = function() {
        $("#privacy-data-disposal-term").change(function() {
            var $option = $(this.selectedOptions[0]);
            var value = $option.data("value");
            if (value) {
                $("#privacy-data-disposal-term-custom").val(value).prop("readonly", true);
            } else {
                $("#privacy-data-disposal-term-custom").val("").prop("readonly", false);
            }
        });
    };

    initializers.Payment = function() {
        function updateTotalCost() {
            var subjects = parseInt($("#cost-subjects").val());
            var averageCost = parseFloat($("#cost-average").val());
            var totalCost = subjects * averageCost;
            $("#cost-predicted").val(isNaN(totalCost) ? "" : totalCost.toFixed(2));
        }

        $("#cost-average,#cost-max-total").change(function() {
            var $input = $(this);
            var val = parseFloat($input.val());
            if (!isNaN(val)) {
                $input.val(val.toFixed(2));
            }
        });

        $("#cost-subjects,#cost-average").change(updateTotalCost);
        updateTotalCost();
    };

    function replaceFormFragment($form, fragment) {
        var url = new URL($form.prop("action"));
        url.hash = fragment;
        $form.prop("action", url.toString());
    }

    $("#confirm-approve-dialog").on("show.bs.modal", function(event) {
        var $button = $(event.relatedTarget);
        replaceFormFragment($(this).parent("form"), $button.data("section"));
        $("#confirm-approve-dialog-role").val($button.data("role"));
    });

    $("#confirm-reject-dialog").on("show.bs.modal", function(event) {
        var $button = $(event.relatedTarget);
        replaceFormFragment($(this).parent("form"), $button.data("section"));
        $("#confirm-reject-dialog-role").val($button.data("role"));
    });

    $("#confirm-submit-dialog").on("show.bs.modal", function(event) {
        var $button = $(event.relatedTarget);
        replaceFormFragment($(this).parent("form"), $button.data("section"));
        $("#confirm-submit-dialog-section").val($button.data("section"));
    });

    $("#confirm-retract-dialog").on("show.bs.modal", function(event) {
        var $button = $(event.relatedTarget);
        replaceFormFragment($(this).parent("form"), $button.data("section"));
        $("#confirm-retract-dialog-section").val($button.data("section"));
    });

    var connection = new signalR.HubConnectionBuilder().withUrl("/ws?proposalId=" + encodeURIComponent(formInfo.proposalId)).build();
    connection.on("UpdateForm", function (_proposalId, timestamp, lastEditedBy, lastEditedOn, sectionsChanged) {
        console.log("Detected changes in the database for the following sections: ", sectionsChanged.join(", "));

        $.when(requestInfo.current).done(function() {
            if (requestInfo.timestamp === timestamp) {
                console.log("Form already up to date.");
                return;
            }

            console.log("Form out of date. Old timestamp: " + requestInfo.timestamp + ". New timestamp: " + timestamp);

            var $sectionNav = $("#section-nav");
            $.get({
                url: $sectionNav.data("refresh-url"),
                dataType: "html"
            }).done(function(html) {
                $sectionNav.replaceWith(html);
                $("#form-view").scrollspy("refresh");
            });

            $.when.apply(null, sectionsChanged.map(function(sectionId) {
                var $section = $("#" + sectionId);

                console.log("  Section outdated: " + sectionId + ". Retrieving update.");

                return $.get({
                    url: $section.data("refresh-url"),
                    dataType: "html"
                }).then(function(html) {
                    var $focusedElement = $(document.activeElement);
                    var focusedName = $focusedElement.prop("name");
                    var focusedSelStart = $focusedElement.prop("selectionStart");
                    var focusedSelEnd = $focusedElement.prop("selectionEnd");

                    var $newSection = $(html);
                    $section.replaceWith($newSection);
                    initializeSection($newSection);

                    if (focusedName) {
                        $focusedElement = $section.find("[name='" + focusedName + "']");
                        if ($focusedElement) {
                            $focusedElement.focus();
                            $focusedElement.prop("selectionStart", focusedSelStart);
                            $focusedElement.prop("selectionEnd", focusedSelEnd);
                        }
                    }

                    console.log("  Section updated: " + sectionId);
                });
            })).done(function() {
                $("#form-view").scrollspy("refresh");

                var $elem = $("<div></div>").append(
                    document.createTextNode("The proposal was updated by " + lastEditedBy + ".")
                );

                if (sectionsChanged.length > 0) {
                    $elem
                        .append(" The following section(s) were affected: ")
                        .append(sectionsChanged.map(function(sectionId) {
                            return $("<div></div>")
                                .append($("<a></a>", { href: "#" + sectionId }).text($("#" + sectionId).data("name")))
                                .html();
                        }).join(", "))
                        .append(".");
                }

                showToast("External update", $elem.html(), moment(lastEditedOn));
            });
        });
    });

    connection.onclose(function() {
        if (!connection) {
            return;
        }

        var title = "Connection to page lost";
        var message = "Lost connection to page. Make sure you are connected to the internet or try again later.";
        showErrorModal(title, message);
    });

    connection.start().then(function() {
        $(window).on("beforeunload", function() {
            connection.stop();
            connection = null;
        });
    }).catch(function() {
        var title = "Could not connect to page";
        var message = "Could not connect to page. Make sure you are connected to the internet or try again later.";
        showErrorModal(title, message);
    });

    // Init
    $(".form-section").each(function() {
        initializeSection($(this));
    });
});