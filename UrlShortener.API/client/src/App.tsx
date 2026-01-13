import { Routes, Route, Navigate } from "react-router-dom";
import SubscriptionsPage from "./pages/SubscriptionsPage";
import PricingPage from "./pages/PricingPage.tsx";
import HeroShortenerPage from "./pages/HeroShortenerPage.tsx";
import LoginPage from "./pages/LoginPage.tsx";
import SignUpPage from "./pages/SignUpPage.tsx";
import ProfilePage from "./pages/ProfilePage.tsx";
import LinkDetailsPage from "./pages/LinkDetailsPage.tsx";

export default function App() {
    return (
        <div className="min-h-screen">
            <Routes>
                <Route path="/subscriptions" element={<SubscriptionsPage />} />
                <Route path="/pricing" element={<PricingPage />} />
                <Route path="/" element={<HeroShortenerPage />} />
                <Route path="*" element={<Navigate to="/" replace />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/signup" element={<SignUpPage />} />
                <Route path="/profile" element={<ProfilePage />} />
                <Route path="/details" element={<LinkDetailsPage />} />
            </Routes>
        </div>
    );
}
