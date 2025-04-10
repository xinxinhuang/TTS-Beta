# TeeTime App

A golf tee time management application built with Next.js and styled with Tailwind CSS.

## Local Development

First, install dependencies:

```bash
npm install
# or
yarn install
```

Then, run the development server:

```bash
npm run dev
# or
yarn dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

## Deployment to Railway

This project is configured for easy deployment to [Railway](https://railway.app/).

### Deploying to Railway

1. Create a new project on Railway
2. Connect your GitHub repository or use the Railway CLI to deploy
3. Railway will automatically detect the Dockerfile and deploy your application

### Using Railway CLI

```bash
# Install Railway CLI
npm i -g @railway/cli

# Login to Railway
railway login

# Link to your project
railway link

# Deploy your app
railway up
```

### Environment Variables

Make sure to set up the necessary environment variables in Railway:

- `NODE_ENV`: Set to `production` for production deployments
- `NEXT_PUBLIC_API_URL`: The URL of your API (if applicable)

Copy the variables from `.env.example` to set up your project.

## Features

- Responsive design with Tailwind CSS
- Reusable UI components
- Modern Next.js architecture
- Optimized for deployment on Railway

## Learn More

- [Next.js Documentation](https://nextjs.org/docs)
- [Tailwind CSS Documentation](https://tailwindcss.com/docs)
- [Railway Documentation](https://docs.railway.app/) 