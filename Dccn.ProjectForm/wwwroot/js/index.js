"use strict";

$(function() {
    $("#delete-proposal-dialog").on("show.bs.modal", function(event) {
        var $button = $(event.relatedTarget);
        var id = $button.data("proposalId");

        $("#delete-proposal-id").val(id);
    });

    $("#export-proposal-dialog").on("show.bs.modal", function(event) {
        var $button = $(event.relatedTarget);
        var id = $button.data("proposal-id");
        var sourceId = $button.data("sourceId");

        $("#export-proposal-id").val(id);
        $("#export-proposal-source-id").val(sourceId);
    });

    var tableLayout =
        "<'row'<'col-sm-12'<'toolbar'>>>" +
        "<'row'<'col-sm-12'tr>>" +
        "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>";

    function initializeToolbar(table, tableData) {
        $(".toolbar", table.container()).replaceWith(function() {
            var $toolbar = $($("#toolbar-template").render(null, tableData));

            $(".filter-count", $toolbar).change(function() {
                table.page.len(parseInt($(this).val())).draw();
            }).change();

            $(".filter-status", $toolbar).change(function() {
                table.column(".status").search($(this).val(), true, false).draw();
            }).val(tableData.statusFilter ? "^" + tableData.statusFilter + "$" : "").change();

            $(".filter-role", $toolbar).change(function() {
                table.column(".role-type").search($(this).val(), true, false).draw();
            }).change();

            $(".filter-search", $toolbar).on("input", function() {
                table.search($(this).val()).draw();
            });

            return $toolbar;
        });
    }

    $("table[data-row-type='proposal']").each(function() {
        var tableData = $(this).data();


        var options = {
            dom: tableLayout,
            order: [],
            autoWidth: false,
            columnDefs: [
                {
                    targets: "status",
                    render: {
                        display: function(_data, _type, row) {
                            return $("#proposal-status-template").render(row, tableData);
                        }
                    }
                },
                {
                    targets: "controls",
                    render: {
                        display: function(_data, _type, row) {
                            return $("#proposal-controls-template").render(row, tableData);
                        }
                    }
                }
            ],
            drawCallback: function() {
                $(".dataTables_paginate > .pagination").addClass("pagination-sm");
            }
        };

        if (tableData.ajaxUrl) {
            options.ajax = {
                url: tableData.ajaxUrl,
                dataSrc: ""
            };
        }

        var table = $(this).DataTable(options).table();
        initializeToolbar(table, tableData);

        setInterval(function () {
            table.ajax.reload(null, false);
        }, 10000);
    });

    $("table[data-row-type='approval']").each(function() {
        var tableData = $(this).data();

        var options = {
            dom: tableLayout,
            order: [],
            autoWidth: false,
            columnDefs: [
                {
                    targets: "role",
                    render: {
                        display: function(_data, _type, row) {
                            return $("#approval-role-template").render(row, tableData);
                        }
                    }
                },
                {
                    targets: "status",
                    render: {
                        display: function(_data, _type, row) {
                            return $("#approval-status-template").render(row, tableData);
                        }
                    }
                },
                {
                    targets: "controls",
                    render: {
                        display: function(_data, _type, row) {
                            return $("#approval-controls-template").render(row, tableData);
                        }
                    }
                }
            ],
            drawCallback: function() {
                $(".dataTables_paginate > .pagination").addClass("pagination-sm");
            }
        };

        if (tableData.groupRows) {
//            options.columnDefs.push({
//                targets: ["title", "owner-name"],
//                render: {
//                    display: function(data) {
//                        return $("<div>").append($("<span>").addClass("text-muted").text(data)).html();
//                    }
//                }
//            });
            options.rowGroup = {
                dataSrc: "proposalId",
                startRender: function(rows) {
                    var data = rows.data()[0];
                    return $("<tr>").append($("<td>", { colspan: 5 }).addClass("text-center").text(data.proposalTitle));
                }
            };
        }

        if (tableData.ajaxUrl) {
            options.ajax = {
                url: tableData.ajaxUrl,
                dataSrc: ""
            };
        }

        var table = $(this).DataTable(options).table();
        initializeToolbar(table, tableData);

        setInterval(function () {
            table.ajax.reload(null, false);
        }, 10000);
    });

    var activeTab = $("#tabs").data("active-tab");
    if (activeTab) {
        $(activeTab).tab("show");
    } else {
        $("#tabs > a:first-child").tab("show");
    }
});
