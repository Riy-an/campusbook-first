/**
 * CampusBook — Neumorphism (Soft UI) & Sidebar Interaction Controller
 */

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        initSidebar();
        initNeumorphicInteractions();
    });

    /**
     * Sidebar Navigation (Desktop Collapsible + Mobile Responsive Drawer)
     */
    function initSidebar() {
        const sidebar = document.getElementById("cbSidebar");
        const backdrop = document.getElementById("cbSidebarBackdrop");
        const desktopToggle = document.getElementById("cbDesktopSidebarToggle");
        const mobileToggle = document.getElementById("cbMobileSidebarToggle");
        const body = document.body;

        if (!sidebar) return;

        // 1. Restore Desktop Sidebar state from localStorage
        const isCollapsed = localStorage.getItem("cb-sidebar-collapsed") === "true";
        if (isCollapsed && window.innerWidth >= 992) {
            body.classList.add("cb-sidebar-collapsed");
            updateToggleIcon(true);
        }

        // 2. Desktop Sidebar Toggle (Collapse / Expand)
        if (desktopToggle) {
            desktopToggle.addEventListener("click", function (e) {
                e.preventDefault();
                const willCollapse = !body.classList.contains("cb-sidebar-collapsed");
                body.classList.toggle("cb-sidebar-collapsed", willCollapse);
                localStorage.setItem("cb-sidebar-collapsed", willCollapse);
                updateToggleIcon(willCollapse);
            });
        }

        function updateToggleIcon(collapsed) {
            if (!desktopToggle) return;
            const icon = desktopToggle.querySelector(".cb-toggle-icon");
            if (icon) {
                icon.textContent = collapsed ? "▶" : "◀";
            }
            desktopToggle.setAttribute("title", collapsed ? "Expand Sidebar" : "Collapse Sidebar");
        }

        // 3. Mobile Drawer Toggle (Open / Close)
        if (mobileToggle) {
            mobileToggle.addEventListener("click", function (e) {
                e.preventDefault();
                openMobileSidebar();
            });
        }

        if (backdrop) {
            backdrop.addEventListener("click", function () {
                closeMobileSidebar();
            });
        }

        function openMobileSidebar() {
            sidebar.classList.add("show");
            if (backdrop) backdrop.classList.add("show");
            body.style.overflow = "hidden"; // Prevent body scrolling while drawer is open
        }

        function closeMobileSidebar() {
            sidebar.classList.remove("show");
            if (backdrop) backdrop.classList.remove("show");
            body.style.overflow = "";
        }

        // 4. Close mobile drawer on ESC key
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && sidebar.classList.contains("show")) {
                closeMobileSidebar();
            }
        });

        // 5. Close mobile drawer when clicking a navigation link
        const navLinks = sidebar.querySelectorAll(".cb-nav-link");
        navLinks.forEach(function (link) {
            link.addEventListener("click", function () {
                if (window.innerWidth < 992) {
                    closeMobileSidebar();
                }
            });
        });

        // 6. Handle window resize gracefully
        window.addEventListener("resize", function () {
            if (window.innerWidth >= 992) {
                closeMobileSidebar();
                const savedCollapsed = localStorage.getItem("cb-sidebar-collapsed") === "true";
                body.classList.toggle("cb-sidebar-collapsed", savedCollapsed);
                updateToggleIcon(savedCollapsed);
            } else {
                body.classList.remove("cb-sidebar-collapsed");
            }
        });
    }

    /**
     * Neumorphic Soft UI Micro-interactions
     */
    function initNeumorphicInteractions() {
        // Auto-dismiss alert messages after 5 seconds if not closed
        const autoDismissAlerts = document.querySelectorAll(".alert-dismissible");
        autoDismissAlerts.forEach(function (alert) {
            setTimeout(function () {
                try {
                    const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
                    if (bsAlert) bsAlert.close();
                } catch (e) {
                    // Fallback
                    alert.classList.remove("show");
                    setTimeout(() => alert.remove(), 250);
                }
            }, 5500);
        });

        // Enhance interactive cards with gentle pressed sensation on mousedown
        const interactiveCards = document.querySelectorAll(".card-campus, .stat-card, .btn-neu");
        interactiveCards.forEach(function (el) {
            el.addEventListener("mousedown", function () {
                this.style.transition = "transform 0.1s ease";
            });
            el.addEventListener("mouseup", function () {
                this.style.transition = "";
            });
        });
    }
})();
