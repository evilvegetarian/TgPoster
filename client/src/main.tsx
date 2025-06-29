import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import { createBrowserRouter, RouterProvider } from "react-router-dom";

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import ErrorPage from "@/pages/errorPage.tsx";
import {Layout} from "@/layout.tsx";
import {RegisterPage} from "@/pages/registerPage.tsx";
import {LoginPage} from "@/pages/loginPage.tsx";
import {RegistrationSuccessPage} from "@/pages/registrationSuccessPage.tsx";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 1000 * 60 * 5,
    },
  },
});

const router = createBrowserRouter([
  {
    path: "/",
    element: <Layout />,
    children: [
      {
        index: true,
        element: <RegisterPage />,
      },
      {
        path: "login",
        element: <LoginPage />,
      },
      {
        path: "register",
        element: <RegisterPage />,
      },
      {
        path: "register/success",
        element: <RegistrationSuccessPage />,
      },
    ],
    errorElement: <ErrorPage/>
  },
]);

const rootElement = document.getElementById('root')!;
const root = createRoot(rootElement);

root.render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
        <ReactQueryDevtools initialIsOpen={false} />
      </QueryClientProvider>
    </StrictMode>,
);
